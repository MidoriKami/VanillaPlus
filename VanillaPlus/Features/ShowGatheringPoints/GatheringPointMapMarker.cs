using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Overlay.MapOverlay;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.ShowGatheringPoints;

public unsafe class GatheringPointMapMarker : MapMarkerNode {
    public required int ObjectIndex { get; set; }
    
    public GatheringPointMapMarker() {
        Size = new Vector2(32.0f, 32.0f);
    }

    protected override void OnUpdate() {
        IsVisible = false;

        var gatheringPoint = EventObjectManager.Instance()->EventObjects[ObjectIndex].Value;
        if (gatheringPoint is null) return;

        if (gatheringPoint->ObjectKind is not ObjectKind.GatheringPoint) return;
        if (!gatheringPoint->GetIsTargetable()) return;
        
        var objectPosition = new Vector2(gatheringPoint->Position.X, gatheringPoint->Position.Z);
        var objectName = gatheringPoint->NameString;

        IsVisible = true;
        Position = objectPosition;
        MapId = AgentMap.Instance()->CurrentMapId;
        TextTooltip = objectName;
        IconId = GetIconId(gatheringPoint->BaseId);
    }

    private uint GetIconId(uint gatheringPointId) {
        var gatheringPoint = Services.DataManager.GetExcelSheet<GatheringPoint>().GetRow(gatheringPointId);
        var gatheringPointBase = Services.DataManager.GetExcelSheet<GatheringPointBase>().GetRow(gatheringPoint.GatheringPointBase.RowId);

        return gatheringPointBase.GatheringType.RowId switch
        {
            0 => 60438,
            1 => 60437,
            2 => 60433,
            3 => 60432,
            5 => 60445,
            _ => throw new Exception($"Unknown Gathering Type: {gatheringPointBase.GatheringType.RowId}"),
        };
    }
}
