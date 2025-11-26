using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DebugGameModification;

#if DEBUG
/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Debug GameModification",
        Description = "A module for playing around and testing VanillaPlus features",
        Type = ModificationType.Debug,
        Authors = [ "YourNameHere" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override void OnEnable() {
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, string.Empty, Handler);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, string.Empty, Handler);
        // Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, string.Empty, Handler);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, string.Empty, Handler);
        // Services.AddonLifecycle.RegisterListener(AddonEvent.PostReceiveEvent, string.Empty, Handler);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostOpen, string.Empty, Handler);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostClose, string.Empty, Handler);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostShow, string.Empty, Handler);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostHide, string.Empty, Handler);
        Services.PluginLog.Debug("Listeners Registered!");
    }

    private void Handler(AddonEvent type, AddonArgs args) {
        Services.PluginLog.Debug($"[{type}] {args.AddonName}");
    }

    public override void OnDisable() {
        Services.AddonLifecycle.UnregisterListener(Handler);
    }
}
#endif
