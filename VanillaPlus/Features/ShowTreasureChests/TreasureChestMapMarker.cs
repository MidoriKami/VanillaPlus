using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Overlay.MapOverlay;

namespace VanillaPlus.Features.ShowTreasureChests;

public sealed unsafe class TreasureChestMapMarker : MapMarkerNode {
    public required int ObjectIndex { get; init; }

    public TreasureChestMapMarker() {
        IconId = 60003;
        Size = new Vector2(32.0f, 32.0f);
    }

    protected override void OnUpdate() {
        IsVisible = false;

        var localChara = Services.ObjectTable.LocalPlayer;
        if (localChara is null) return; 
        
        var treasureObject = EventObjectManager.Instance()->EventObjects[ObjectIndex].Value;
        if (treasureObject is null) return;

        if (treasureObject->ObjectKind is not ObjectKind.Treasure) return;
        if (!treasureObject->GetIsTargetable()) return;
        if (Vector3.Distance(treasureObject->Position, localChara.Position) > 150.0f) return;
        
        var objectPosition = new Vector2(treasureObject->Position.X, treasureObject->Position.Z);
        var objectName = treasureObject->NameString;

        IsVisible = true;
        Position = objectPosition;
        MapId = AgentMap.Instance()->CurrentMapId;
        TextTooltip = objectName;
    }
}
