using KamiToolKit.Overlay;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// Displays the "Open Duty Loot" button near the active duty info
/// </summary>
public unsafe class DutyLootInDutyUiController {
    private OverlayController? overlayController;
    private DutyLootInDutyButtonNode? lootButtonNode;
    private bool isValidTerritory;

    public Action? OnButtonClicked { get; init; }

    public void OnEnable() {
        overlayController = new OverlayController();

        Services.ClientState.TerritoryChanged += OnTerritoryChanged;

        Services.Framework.RunOnFrameworkThread(AddOverlayNodes);
    }

    private void AddOverlayNodes() {
        if (overlayController is null) return;

        lootButtonNode = new DutyLootInDutyButtonNode {
            OnClick = () => OnButtonClicked?.Invoke(),
            TryShow = ShouldShowButton()
        };
        overlayController.AddNode(lootButtonNode);
    }

    public void OnDisable() {
        overlayController?.Dispose();
        overlayController = null;

        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
    }

    private void OnTerritoryChanged(ushort territoryRow) {
        if (lootButtonNode is null) return;

        lootButtonNode.TryShow = ShouldShowButton(territoryRow);
    }

    private bool ShouldShowButton(ushort? territoryRow = null) {
        var territory = territoryRow ?? Services.ClientState.TerritoryType;
        var territoryType = Services.DataManager.GetExcelSheet<TerritoryType>().GetRow(territory);
        var contentTypeId = territoryType.ContentFinderCondition.Value.ContentType.RowId;
        return contentTypeId is 2 or 4 or 5; // Dungeon, Trial, Raid (including Alliance)
    }
}
