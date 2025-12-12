using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes.Controllers;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// Displays the "Open Duty Loot" button near the active duty info
/// </summary>
internal unsafe class DutyLootInDutyUiController {
    private AddonController<AddonToDoList>? dutyInfo;
    private TextureButtonNode? lootButtonNode;

    public Action? OnButtonClicked { get; init; }

    public void OnEnable() {
        dutyInfo = new AddonController<AddonToDoList>("_ToDoList");
        dutyInfo.OnAttach += AttachNodes;
        dutyInfo.OnDetach += DetachNodes;
        dutyInfo.Enable();

        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void OnDisable() {
        dutyInfo?.Dispose();
        dutyInfo = null;

        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
    }

    private void AttachNodes(AddonToDoList* addon) {
        var dutyNameContainer = addon->AtkUnitBase.GetNodeById<AtkComponentNode>(4);
        if (dutyNameContainer is null) return;

        lootButtonNode = new TextureButtonNode {
            TexturePath = "ui/uld/Inventory.tex",
            TextureCoordinates = new Vector2(90.0f, 125.0f),
            TextureSize = new Vector2(32.0f, 32.0f),
            IsVisible = false,
            Position = new Vector2(260f - 20.0f - 4.0f, 29f),
            Size = new Vector2(20.0f, 20.0f),
            TooltipString = "[VanillaPlus] View Loot that can be earned in this duty.",
            OnClick = () => OnButtonClicked?.Invoke(),
        };
        lootButtonNode.AttachNode(dutyNameContainer, NodePosition.AfterAllSiblings);

    }

    private void DetachNodes(AddonToDoList* addon) {
        lootButtonNode?.Dispose();
        lootButtonNode = null;
    }

    private void OnTerritoryChanged(ushort territoryRow) {
        if (lootButtonNode is null) return;

        var territoryType = Services.DataManager.GetExcelSheet<TerritoryType>().GetRow(territoryRow);
        var contentTypeId = territoryType.ContentFinderCondition.Value.ContentType.RowId;
        lootButtonNode.IsVisible = contentTypeId is 2 or 4 or 5; // Dungeon, Trial, Raid (including Alliance)
    }
}
