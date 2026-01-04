using System;
using System.IO;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public abstract class GameModificationConfig<T> : ISavable where T : GameModificationConfig<T>, new() {
    protected abstract string FileName { get; }
    public virtual int Version => 1;

    public static T Load() {
        var configFileName = new T().FileName;

        Services.PluginLog.Debug($"Loading Config {configFileName}.config.json");
        var loadedConfig = Config.LoadConfig<T>(configFileName);

        try {
            var fileInfo = new FileInfo(Path.Combine(Config.ConfigPath, $"{configFileName}.config.json"));
            var fileText = File.ReadAllText(fileInfo.FullName);
            var jObject = JObject.Parse(fileText);
            var version = jObject[nameof(Version)]?.ToObject<int>();

            if (loadedConfig.TryMigrateConfig(version, jObject)) {
                Services.PluginLog.Debug($"[{configFileName}] Successfully migrated config file");
                loadedConfig.Save();
            }
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, $"Failed to migrate config file for {configFileName}, loading default config.");
        }
  
        return loadedConfig;
    } 
    
    public void Save() {
        Services.PluginLog.Debug($"Saving Config {FileName}.config.json");
        Config.SaveConfig(this, $"{FileName}.config.json");
        OnSave?.Invoke();
    }

    [JsonIgnore] public Action? OnSave { get; set; }

    /// <summary>
    /// Function for migrating old config values to new values.
    /// </summary>
    /// <param name="fileVersion">Number indicating current file version, null if saved before this system was added.</param>
    /// <param name="jObject">The JSON properties of the loaded config file</param>
    /// <returns>true to indicate migration success, false to indicate migration is not needed.</returns>
    protected virtual bool TryMigrateConfig(int? fileVersion, JObject jObject) => false;
}
