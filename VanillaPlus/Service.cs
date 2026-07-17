using System;
using Dalamud.Plugin.Services;

namespace VanillaPlus;

/// <summary>
/// Generic service getter, allows you to get any dalamud service you need via the interface typename, for example IAddonLifecycle or IPluginLog.
/// </summary>
public static class Service<T> where T : class, IDalamudService {
    private static T? ServiceInstance
        => field ??= VanillaPlus.PluginInterface.GetService(typeof(T)) as T;

    /// <summary>
    /// Gets a reference to the dalamud service via type.
    /// </summary>
    public static T Get()
        => ServiceInstance ?? throw new InvalidOperationException($"Service {typeof(T).Name} not found.");
}
