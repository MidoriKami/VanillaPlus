using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Extensions;

public static class AddonLifecycleExtensions {
    public static void LogAddon(this IAddonLifecycle addonLifecycle, string addonName) {
        addonLifecycle.RegisterListener(AddonEvent.PreSetup, addonName, Logger);
        addonLifecycle.RegisterListener(AddonEvent.PreRefresh, addonName, Logger);
        addonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, addonName, Logger);
        addonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, addonName, Logger);
        addonLifecycle.RegisterListener(AddonEvent.PreFinalize, addonName, Logger);
    }

    private static void Logger(AddonEvent type, AddonArgs args)
        => Services.PluginLog.Debug($"{args.AddonName} called {type}");

    public static void UnLogAddon(this IAddonLifecycle addonLifecycle, string addonName) {
        addonLifecycle.UnregisterListener(AddonEvent.PreSetup, addonName, Logger);
        addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, addonName, Logger);
        addonLifecycle.UnregisterListener(AddonEvent.PreReceiveEvent, addonName, Logger);
        addonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, addonName, Logger);
        addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, addonName, Logger);
    }
}
