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
        => Services.AddonLifecycle.RegisterListener(AddonEvent.PreReceiveEvent, "_CharaSelectListMenu", OnCharacterListReceiveEvent);

    public override void OnDisable()
        => Services.AddonLifecycle.UnregisterListener(OnCharacterListReceiveEvent);

    private static void OnCharacterListReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs receiveEventArgs) return;
        if ((AtkEventType)receiveEventArgs.AtkEventType is not AtkEventType.MouseClick) return;
        if (receiveEventArgs.EventParam is < 5 or > 12) return;

        Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", SelectYesNoHandler);
    }

    private static void SelectYesNoHandler(AddonEvent _, AddonArgs yesNoArgs) {
        var addon = yesNoArgs.GetAddon<AddonSelectYesno>();

        if (addon->AtkUnitBase.GetCallbackHandlerInfo() is { AgentId: AgentId.Lobby, EventKind: 3 }) {
            addon->YesButton->SetEnabledState(false);
            addon->AtkUnitBase.FireCallbackCommand([ 0 ]);
        }

        Services.AddonLifecycle.UnregisterListener(SelectYesNoHandler);
    }
}
