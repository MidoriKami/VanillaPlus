using System.Collections.Generic;
using System.Linq;

namespace VanillaPlus.Classes;

public class PluginCompatabilityModule(string pluginName) : CompatabilityModule {
    public override string TargetModule => pluginName;
    public override string TargetPluginInternalName => pluginName;

    protected override List<string> GetTargetPluginLoadedModules()
        => Services.PluginInterface.InstalledPlugins
                   .Where(plugin => plugin is { IsLoaded: true })
                   .Select(plugin => plugin.InternalName)
                   .ToList();
}
