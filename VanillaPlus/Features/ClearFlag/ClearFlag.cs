using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Events.EventDataTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.Controllers;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ClearFlag;

public unsafe class ClearFlag : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_ClearFlag"),
        Description = Strings("ModificationDescription_ClearFlag"),
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private AddonController? minimapController;
    private IAddonEventHandle? minimapMouseClick;
    
    public override void OnEnable() {
        minimapController = new AddonController("_NaviMap");

        minimapController.OnAttach += addon => {
            var collisionNode = addon->GetNodeById<AtkCollisionNode>(19);
            if (collisionNode is null) return;

            collisionNode->DrawFlags |= (uint)DrawFlags.ClickableCursor;

            minimapMouseClick = Services.AddonEventManager.AddEvent((nint)addon, (nint)collisionNode, AddonEventType.MouseClick, OnMiniMapMouseClick);
        };

        minimapController.OnDetach += addon => {
            Services.AddonEventManager.RemoveEventNullable(minimapMouseClick);
            
            var collisionNode = addon->GetNodeById<AtkCollisionNode>(19);
            if (collisionNode is null) return;

            collisionNode->DrawFlags &= ~(uint)DrawFlags.ClickableCursor;
            
        };

        minimapController.Enable();
    }

    public override void OnDisable() {
        minimapController?.Dispose();
        minimapController = null;
    }

    private static void OnMiniMapMouseClick(AddonEventType addonEventType, AddonEventData data) {
        if (data.IsRightClick && AgentMap.Instance()->FlagMarkerCount is not 0) {
            AgentMap.Instance()->FlagMarkerCount = 0;
        }
    }
}
