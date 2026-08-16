using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public class SystemConfiguration {
    public int Version = 1;

    public HashSet<string> EnabledModifications = [];
    public bool IsDebugMode = false;
    public string CurrentSearch = string.Empty;
    public DateTime LastSeasonalNotice = DateTime.MinValue;
    public bool PersistSearch = false;
    public bool SafeMode = false;

    public static async Task<SystemConfiguration> Load() {
        IPluginLog.Get().Debug("Loading system.config.json");
        return await Config.LoadConfig<SystemConfiguration>("system.config.json");
    }

    private readonly SemaphoreSlim saveLock = new(1, 1);
    private bool pendingSave;

    public Task Save() {
        IPluginLog.Get().Verbose("Queuing Save for system.config.json");

        Interlocked.Exchange(ref pendingSave, true);

        return Task.Run(SaveAsync);
    }

    private async Task SaveAsync() {
        if (!await saveLock.WaitAsync(0)) {
            IPluginLog.Get().Verbose("Save in progress for system.config.json, skipping save.");
            return;
        }

        try {
            while (Interlocked.Exchange(ref pendingSave, false)) {
                IPluginLog.Get().Debug("Saving Config system.config.json");
                await Config.SaveConfig(this, "system.config.json");
            }
        }
        catch (Exception e) {
            IPluginLog.Get().Error(e, "Failed to save system.config.json");
        }
        finally {
            saveLock.Release();
        }
    }
}
