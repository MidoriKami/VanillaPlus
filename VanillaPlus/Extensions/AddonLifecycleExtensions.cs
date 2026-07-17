using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

/// <summary>
/// Extensions for Dalamud's <see cref="IAddonLifecycle"/> service for triggering events for native window events.
/// </summary>
public static class AddonLifecycleExtensions {
    extension(IAddonLifecycle addonLifecycle) {

        /// <summary>
        /// Logs the specified event for all addons.
        /// </summary>
        public void LogAll(params AddonEvent[] addonEvents) {
            foreach (var addonEvent in addonEvents) {
                addonLifecycle.RegisterListener(addonEvent, string.Empty, LogEventMessage);
            }
        }

        /// <summary>
        /// Disables global logging for the specified events.
        /// </summary>
        public void UnlogAll(params AddonEvent[] addonEvents) {
            foreach (var addonEvent in addonEvents) {
                addonLifecycle.UnregisterListener(addonEvent, string.Empty, LogEventMessage);
            }
        }

        /// <summary>
        /// Logs the specified events for the specified addon. Logs a standard set of events if none provided.
        /// </summary>
        public void LogAddon(string addonName, params AddonEvent[] loggedEvents) {
            if (loggedEvents.Length is 0) {
                loggedEvents = [
                    AddonEvent.PreSetup,
                    AddonEvent.PreFinalize,
                    AddonEvent.PreRequestedUpdate,
                    AddonEvent.PreRefresh,
                    AddonEvent.PreReceiveEvent,
                    AddonEvent.PreOpen,
                    AddonEvent.PreClose,
                    AddonEvent.PreShow,
                    AddonEvent.PreHide,
                ];
            }

            ActiveLoggers.TryAdd(addonName, loggedEvents.ToList());
            foreach (var eventType in loggedEvents) {
                addonLifecycle.RegisterListener(eventType, addonName, LogEventMessage);
            }
        }

        /// <summary>
        /// Disables logging for the specified addon, only disables events from <see cref="LogAddon"/>
        /// </summary>
        public void UnLogAddon(string addonName) {
            if (!ActiveLoggers.TryGetValue(addonName, out var loggedModules)) return;

            foreach (var loggedModule in loggedModules) {
                addonLifecycle.UnregisterListener(loggedModule, addonName, LogEventMessage);
            }
        }
    }

    private static readonly Dictionary<string, List<AddonEvent>> ActiveLoggers = [];

    private static void LogEventMessage(AddonEvent type, AddonArgs args) {
        var logString = $"[{args.AddonName}] [{type}] ";

        switch (type) {
            case AddonEvent.PreSetup or AddonEvent.PostSetup when args is AddonSetupArgs setupArgs:
                logString += $"AtkValue Count: {setupArgs.AtkValueCount}";
                break;

            case AddonEvent.PreRefresh or AddonEvent.PostRefresh when args is AddonRefreshArgs refreshArgs:
                logString += $"AtkValue Count: {refreshArgs.AtkValueCount}";
                break;

            case AddonEvent.PreReceiveEvent or AddonEvent.PostReceiveEvent when args is AddonReceiveEventArgs receiveEventArgs:
                logString += $"Event Type: {(AtkEventType)receiveEventArgs.AtkEventType}, Event Param: {receiveEventArgs.EventParam}";
                break;

            case AddonEvent.PreClose or AddonEvent.PostClose when args is AddonCloseArgs closeArgs:
                logString += $"Fire Close Callback: {closeArgs.FireCallback}";
                break;

            case AddonEvent.PreShow or AddonEvent.PostShow when args is AddonShowArgs showArgs:
                logString += $"Silence Open SFX: {showArgs.SilenceOpenSoundEffect}, UnsetShowHideFlags: 0x{showArgs.UnsetShowHideFlags:X}";
                break;

            case AddonEvent.PreHide or AddonEvent.PostHide when args is AddonHideArgs hideArgs:
                logString += $"Fire Hide Callback: {hideArgs.CallHideCallback}, SetShowHideFlags: 0x{hideArgs.SetShowHideFlags:X}";
                break;

            case AddonEvent.PreFocusChanged or AddonEvent.PostFocusChanged when args is AddonFocusChangedArgs focusChangedArgs:
                logString += $"Should Focus: {focusChangedArgs.ShouldFocus}";
                break;

            // RequestedUpdate doesn't have any logging relevant data
            case AddonEvent.PreRequestedUpdate or AddonEvent.PostRequestedUpdate when args is AddonRequestedUpdateArgs:

            // Standard Addon Events
            case AddonEvent.PreUpdate or AddonEvent.PostUpdate:
            case AddonEvent.PreDraw or AddonEvent.PostDraw:
            case AddonEvent.PreFinalize:
            case AddonEvent.PreOpen or AddonEvent.PostOpen:
            case AddonEvent.PreMove or AddonEvent.PostMove:
            case AddonEvent.PreMouseOver or AddonEvent.PostMouseOver:
            case AddonEvent.PreMouseOut or AddonEvent.PostMouseOut:
            case AddonEvent.PreFocus or AddonEvent.PostFocus:
                // Don't add any additional data
                break;
        }

        IPluginLog.Get().Info(logString.Trim());
    }
}
