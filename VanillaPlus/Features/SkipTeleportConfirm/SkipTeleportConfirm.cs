using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using CsAgentId = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentId;

namespace VanillaPlus.Features.SkipTeleportConfirm;

public unsafe class SkipTeleportConfirm : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SkipTeleportConfirm,
        Description = Strings.ModificationDescription_SkipTeleportConfirm,
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
    };

    public override void OnEnable()
        => Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", SelectYesNoHandler);

    public override void OnDisable()
        => Services.AddonLifecycle.UnregisterListener(SelectYesNoHandler);

    private static void SelectYesNoHandler(AddonEvent _, AddonArgs yesNoArgs) {
        var addon = yesNoArgs.GetAddon();

        if (addon->GetCallbackHandlerInfo() is { AgentId: CsAgentId.Map, EventKind: 1 }) {
            addon->SendEvent(AtkEventType.ButtonClick, 0);
        }
    }
}
