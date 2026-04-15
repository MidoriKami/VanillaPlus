using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.SkipLoginConfirm;

public unsafe class SkipLoginConfirm : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Skip Login Confirm",
        Description = "Skips the confirmation window that appears when logging in.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override void OnEnable()
        => Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", SelectYesNoHandler);

    public override void OnDisable()
        => Services.AddonLifecycle.UnregisterListener(SelectYesNoHandler);

    private static void SelectYesNoHandler(AddonEvent _, AddonArgs yesNoArgs) {
        var addon = yesNoArgs.GetAddon<AddonSelectYesno>();

        if (addon->AtkUnitBase.GetCallbackHandlerInfo() is { AgentId: AgentId.Lobby, EventKind: 3 }) {
            var atkEvent = stackalloc AtkEvent[1];
            var atkEventData = stackalloc AtkEventData[1];

            addon->ReceiveEvent(AtkEventType.ButtonClick, 0, atkEvent, atkEventData);
        }
    }
}
