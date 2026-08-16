using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
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

            // Note: This can only handle migrating one step, if v1 -> v2 -> v3 or more, migration is needed,
            // then this would need to be reworked to loop through the versions and have a state based return.
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

    private readonly SemaphoreSlim saveLock = new(1, 1);
    private bool pendingSave;

    public Task Save() {
        IPluginLog.Get().Verbose($"Queuing Save for {FileName}.config.json");

        Interlocked.Exchange(ref pendingSave, true);

        return Task.Run(SaveAsync);
    }

    private async Task SaveAsync() {

        // If we already have a save task running, abort and return.
        if (!await saveLock.WaitAsync(0)) {
            IPluginLog.Get().Verbose($"Save in progress for {FileName}.config.json, skipping save.");
            return;
        }

        try {
            // The while is in-case another save request came in while we are awaiting SaveConfig to complete.
            // It'll allow this same task to handle multiple saves.
            while (Interlocked.Exchange(ref pendingSave, false)) {
                IPluginLog.Get().Debug($"Saving Config {FileName}.config.json");
                await Config.SaveConfig(this, $"{FileName}.config.json");

                // Note, this is being invoked off-thread so any OnSave actions called must be thread safe.
                OnSave?.Invoke();
            }
        }
        catch (Exception e) {
            IPluginLog.Get().Error(e, $"Failed to save {FileName}.config.json");
        }
        finally {
            saveLock.Release();
        }
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
