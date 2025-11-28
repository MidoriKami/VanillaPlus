using System;
using System.Text.Json.Serialization;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public abstract class GameModificationConfig<T> : ISavable where T : GameModificationConfig<T>, new() {
    protected abstract string FileName { get; }

    public static T Load() {
        var configFileName = new T().FileName;
        
        Services.PluginLog.Debug($"Loading Config {configFileName}.config.json");
        return Config.LoadConfig<T>($"{configFileName}.config.json");
    } 
    
    public void Save() {
        Services.PluginLog.Debug($"Saving Config {FileName}.config.json");
        Config.SaveConfig(this, $"{FileName}.config.json");
        OnSave?.Invoke();
    }

    [JsonIgnore] public Action? OnSave { get; set; }
}
