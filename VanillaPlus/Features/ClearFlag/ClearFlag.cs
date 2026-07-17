using System.Threading.Tasks;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Events.EventDataTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ClearFlag;

public class ClearFlag : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ClearFlag,
        Description = Strings.ModificationDescription_ClearFlag,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
    };

    private AddonController? minimapController;
    private IAddonEventHandle? minimapMouseClick;

    public override async Task OnEnableAsync() {
        unsafe {
            minimapController = new AddonController {
                AddonName = "_NaviMap",
                OnSetup = NaviMapSetup,
                OnFinalize = NaviMapFinalize,
            };
        }

        await IFramework.Get().RunSafely(minimapController.Enable);
    }

    public override async Task OnDisableAsync() {
        await IFramework.Get().RunSafely(() => minimapController?.Dispose());
        minimapMouseClick = null;
    }

    private unsafe void NaviMapSetup(AtkUnitBase* addon) {
        var collisionNode = addon->GetNodeById<AtkCollisionNode>(19);
        if (collisionNode is null) return;

        collisionNode->DrawFlags |= (uint)DrawFlags.ClickableCursor;

        minimapMouseClick = IAddonEventManager.Get().AddEvent((nint)addon, (nint)collisionNode, AddonEventType.MouseClick, OnMiniMapMouseClick);
    }

    private unsafe void NaviMapFinalize(AtkUnitBase* addon) {
        IAddonEventManager.Get().RemoveEventNullable(minimapMouseClick);

        var collisionNode = addon->GetNodeById<AtkCollisionNode>(19);
        if (collisionNode is null) return;

        collisionNode->DrawFlags &= ~(uint)DrawFlags.ClickableCursor;
    }

    private static unsafe void OnMiniMapMouseClick(AddonEventType addonEventType, AddonEventData data) {
        if (data.IsRightClick && AgentMap.Instance()->FlagMarkerCount is not 0) {
            AgentMap.Instance()->FlagMarkerCount = 0;
        }
    }
}
