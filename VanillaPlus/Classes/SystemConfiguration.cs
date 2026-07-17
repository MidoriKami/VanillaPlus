using System;
using System.Collections.Generic;
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
        Service<IPluginLog>.Get().Debug("Loading system.config.json");
        return await Config.LoadConfig<SystemConfiguration>("system.config.json");
    }

    public async Task Save() {
        Service<IPluginLog>.Get().Debug("Saving system.config.json");
        await Config.SaveConfig(this, "system.config.json");
    }
}
