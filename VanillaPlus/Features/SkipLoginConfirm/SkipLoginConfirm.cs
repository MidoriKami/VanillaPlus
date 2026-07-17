using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
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

    public override Task OnEnableAsync() {
        IAddonLifecycle.Get().RegisterListener(AddonEvent.PostSetup, "SelectYesno", SelectYesNoHandler);

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        IAddonLifecycle.Get().UnregisterListener(SelectYesNoHandler);

        return Task.CompletedTask;
    }

    private static unsafe void SelectYesNoHandler(AddonEvent _, AddonArgs yesNoArgs) {
        var addon = yesNoArgs.GetAddon();

        if (addon->GetCallbackHandlerInfo() is { AgentId: AgentId.Lobby, EventKind: 3 }) {
            addon->SendEvent(AtkEventType.ButtonClick, 0);
        }
    }
}
