using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.SkipLoginConfirm;

public class SkipLoginConfirm : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SkipLoginConfirm,
        Description = Strings.ModificationDescription_SkipLoginConfirm,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
    };

    public override async Task OnEnableAsync() {
        await Services.Framework.Run(() => {
            Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", SelectYesNoHandler);
        });
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            Services.AddonLifecycle.UnregisterListener(SelectYesNoHandler);
        });
    }

    private static unsafe void SelectYesNoHandler(AddonEvent _, AddonArgs yesNoArgs) {
        var addon = yesNoArgs.GetAddon();

        if (addon->GetCallbackHandlerInfo() is { AgentId: AgentId.Lobby, EventKind: 3 }) {
            addon->SendEvent(AtkEventType.ButtonClick, 0);
        }
    }
}
