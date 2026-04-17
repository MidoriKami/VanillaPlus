using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.SkipLoginConfirm;

public unsafe class SkipLoginConfirm : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SkipLoginConfirm,
        Description = Strings.ModificationDescription_SkipLoginConfirm,
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
        var addon = yesNoArgs.GetAddon();

        if (addon->GetCallbackHandlerInfo() is { AgentId: AgentId.Lobby, EventKind: 3 }) {
            addon->SendEvent(AtkEventType.ButtonClick, 0);
        }
    }
}
