using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using CsAgentId = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentId;

namespace VanillaPlus.Features.SkipTeleportConfirm;

public class SkipTeleportConfirm : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SkipTeleportConfirm,
        Description = Strings.ModificationDescription_SkipTeleportConfirm,
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

        if (addon->GetCallbackHandlerInfo() is { AgentId: CsAgentId.Map, EventKind: 1 }) {
            addon->SendEvent(AtkEventType.ButtonClick, 0);
        }
    }
}
