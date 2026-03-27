using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using KamiToolKit.Overlay.MapOverlay;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;

namespace VanillaPlus.Features.ShowAetherCurrents;

public unsafe class AetherCurrentMapMarker : MapMarkerNode {
    public required RowRef<AetherCurrent> AetherCurrent {
        get;
        init {
            field = value;
            
            if (!Services.DataManager.GetExcelSheet<EObj>().TryGetFirst(rowObject => rowObject.Data.RowId == value.RowId, out var eventObject)) return;
            if (!Services.DataManager.GetExcelSheet<Level>().TryGetFirst(rowObject => rowObject.Object.RowId == eventObject.RowId, out var level)) return;

            MapId = level.Map.RowId;
            Position = new Vector2(level.X, level.Z);
            IconId = 60653;
            TextTooltip = Strings.ShowAetherCurrents_Tooltip;
            Size = new Vector2(32.0f, 32.0f);
        }
    }

    protected override void OnUpdate() {
        IsVisible = !PlayerState.Instance()->IsAetherCurrentUnlocked(AetherCurrent.RowId);
    }
}
