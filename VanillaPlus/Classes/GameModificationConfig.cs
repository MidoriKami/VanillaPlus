using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Newtonsoft.Json.Linq;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public abstract class GameModificationConfig<T> : ISavable where T : GameModificationConfig<T>, new() {
    protected abstract string FileName { get; }
    public virtual int Version => 1;

    public static async Task<T> Load() {
        var configFileName = new T().FileName;

        IPluginLog.Get().Debug($"Loading Config {configFileName}.config.json");
        var loadedConfig = await Config.LoadConfig<T>($"{configFileName}.config.json");

        try {
            var fileInfo = new FileInfo(Path.Combine(Config.ConfigPath, $"{configFileName}.config.json"));

            // Means we didn't have a file until now, and therefore nothing needs to be migrated.
            if (fileInfo is { Exists: false }) {
                return loadedConfig;
            }

            var fileText = await File.ReadAllTextAsync(fileInfo.FullName);
            var jObject = JObject.Parse(fileText);
            var version = jObject[nameof(Version)]?.ToObject<int>();

            if (loadedConfig.TryMigrateConfig(version, jObject)) {
                IPluginLog.Get().Debug($"Successfully migrated $\"{configFileName}.config.json\" to {loadedConfig.Version}");
                await Config.SaveConfig(loadedConfig, $"{configFileName}.config.json");
            }
        }
        catch (Exception e) {
            IPluginLog.Get().Error(e, $"Failed to migrate config file for {configFileName}, loading default config.");
        }

        return loadedConfig;
    }

    public async Task Save() {
        IPluginLog.Get().Debug($"Saving Config {FileName}.config.json");
        await Config.SaveConfig(this, $"{FileName}.config.json");
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
