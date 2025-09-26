using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Events.EventDataTypes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ClearFlag;

public unsafe class ClearFlag : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Clear Flag",
        Description = "Allows you to right click the minimap to clear the currently set flag marker.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private AddonController? minimapController;
    private IAddonEventHandle? minimapMouseOver;
    private IAddonEventHandle? minimapMouseOut;
    private IAddonEventHandle? minimapMouseClick;
    
    public override void OnEnable() {
        minimapController = new AddonController("_NaviMap");

        minimapController.OnAttach += addon => {
            var collisionNode = addon->GetNodeById<AtkCollisionNode>(19);
            if (collisionNode is null) return;

            minimapMouseOver = Services.AddonEventManager.AddEvent((nint)addon, (nint)collisionNode, AddonEventType.MouseOver, OnMiniMapMouseOver);
            minimapMouseOut = Services.AddonEventManager.AddEvent((nint)addon, (nint)collisionNode, AddonEventType.MouseOut, OnMiniMapMouseOut);
            minimapMouseClick = Services.AddonEventManager.AddEvent((nint)addon, (nint)collisionNode, AddonEventType.MouseClick, OnMiniMapMouseClick);
        };

        minimapController.OnDetach += _ => {
            Services.AddonEventManager.RemoveEventNullable(minimapMouseOver);
            Services.AddonEventManager.RemoveEventNullable(minimapMouseOut);
            Services.AddonEventManager.RemoveEventNullable(minimapMouseClick);
        };

        minimapController.Enable();
    }

    public override void OnDisable() {
        minimapController?.Dispose();
        minimapController = null;
    }

    private static void OnMiniMapMouseOver(AddonEventType atkEventType, AddonEventData data)
        => Services.AddonEventManager.SetCursor(AddonCursorType.Clickable);

    private static void OnMiniMapMouseOut(AddonEventType atkEventType, AddonEventData data)
        => Services.AddonEventManager.ResetCursor();

    private static void OnMiniMapMouseClick(AddonEventType atkEventType, AddonEventData data) {
        if (data.IsRightClick() && AgentMap.Instance()->FlagMarkerCount is not 0) {
            AgentMap.Instance()->FlagMarkerCount = 0;
        }
    }
}
