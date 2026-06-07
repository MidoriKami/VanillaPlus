using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using KamiToolKit.MapOverlay;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.ShowAetherCurrents;

public class AetherCurrentMapMarker : MapMarkerNode {
    public required RowRef<AetherCurrent> AetherCurrent {
        get;
        init {
            field = value;

            IconId = 60653;
            TextTooltip = Strings.ShowAetherCurrents_Tooltip;
            Size = new Vector2(32.0f, 32.0f);
        }
    }

    protected override unsafe void OnUpdate() {
        IsVisible = !PlayerState.Instance()->IsAetherCurrentUnlocked(AetherCurrent.RowId);
    }
}
