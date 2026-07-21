using System;
using System.Linq;
using System.Reflection;
using Dalamud.Plugin.Services;

namespace VanillaPlus;

/// <summary>
/// Extension provider for IDalamudService, to add a .Get() method to get an instance of any dalamud service directly from typename.
/// </summary>
/// <code>
/// IPluginLog.Get().Debug(...);
/// </code>
public static class ServiceExtension {

    /// <summary>
    /// Invokes the getter for each valid service once to trigger service construction, and caches the results.
    /// </summary>
    /// <remarks>
    /// This is intended as a hotfix to deadlocks caused by requesting and constructing services in IFramework.Run(...)
    /// </remarks>
    public static void InitializeAllServices() {
        var dalamudServiceType = typeof(IDalamudService);

        var serviceTypes = dalamudServiceType.Assembly.ExportedTypes
            .Where(t => t is { IsAbstract: true } && dalamudServiceType.IsAssignableFrom(t) && t != dalamudServiceType);

        var getMethodDefinition = typeof(ServiceExtension)
            .GetMethod(nameof(Get), BindingFlags.Public | BindingFlags.Static);

        foreach (var type in serviceTypes) {

            // First arg is null because it's the instance to call it on, but this is a static function so that's not needed.
            // Second arg is the function params to pass, Get has no args, so that's also null. Just in-case you were curious.
            getMethodDefinition?.MakeGenericMethod(type).Invoke(null, null);
        }
    }

    /// <summary>
    /// Static class to hold the instance reference.
    /// </summary>
    private static class ServiceInstance<T> where T : class, IDalamudService {
        public static T? Instance => field ??= VanillaPlus.PluginInterface.GetService(typeof(T)) as T;
    }

    /// <summary>
    /// Extension provider to allow you to .Get() from the interface type.
    /// </summary>
    extension<T>(T) where T : class, IDalamudService {
        public static T Get() => ServiceInstance<T>.Instance ?? throw new InvalidOperationException($"Service {typeof(T).Name} not found.");
    }
}
