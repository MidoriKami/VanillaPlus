using System;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using VanillaPlus.Classes;

namespace VanillaPlus;

/// <summary>
/// Add any dalamud services that your modifications require here
/// </summary>
public class Services {
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; } = null!;

    /// <summary>
    /// Gets any available dalamud service via interface typename, for example AddonLifecycle is accessed via IAddonLifecycle.
    /// </summary>
    public static T GetService<T>() where T : class, IDalamudService {
        if (typeof(T) == typeof(IPluginLog)) {
            throw new Exception("Unable to get IPluginLog from GetService, use Services.PluginLog directly instead.");
        }

        return PluginInterface.GetService(typeof(T)) as T ?? throw new InvalidOperationException($"Service {typeof(T).Name} not found.");
    }

    // Wrapper around PluginLog to make module-tagged logging more natural.
    public static PluginLog PluginLog => new(GetService<IPluginLog>());
}
