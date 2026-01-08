using System.Collections.Generic;
using VanillaPlus.Utilities;

namespace VanillaPlus.InternalSystem;

public class SystemConfiguration {
    public int Version = 1;

    public HashSet<string> EnabledModifications = [];
    public bool IsDebugMode = false;

    public static SystemConfiguration Load()
        => Config.LoadConfig<SystemConfiguration>("system.config.json");

    public void Save()
        => Config.SaveConfig(this, "system.config.json");
}
