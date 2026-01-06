using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using Lumina.Excel.Sheets;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview;

public unsafe class DutyLootInDutyButtonNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    private readonly TextureButtonNode buttonNode;

    public Action? OnClick {
        get => buttonNode.OnClick;
        set => buttonNode.OnClick = value;
    }

    private bool shouldShow;

    public DutyLootInDutyButtonNode() {
        buttonNode = new TextureButtonNode {
            TexturePath = "ui/uld/Inventory.tex",
            TextureCoordinates = new Vector2(90.0f, 125.0f),
            TextureSize = new Vector2(32.0f, 32.0f),
            Size = new Vector2(20.0f, 20.0f),
            TextTooltip = Strings.DutyLoot_Tooltip_InDutyButton,
        };
        buttonNode.AttachNode(this);

        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
        OnTerritoryChanged(0);
    }

    protected override void Dispose(bool disposing, bool isNativeDestructor) {
        if (disposing) {
            Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
            
            base.Dispose(disposing, isNativeDestructor);
        }
    }

    protected override void OnUpdate() {
        var dutyInfoAddon = Services.GameGui.GetAddonByName<AddonToDoList>("_ToDoList");
        var dutyInfoPos = dutyInfoAddon->AtkUnitBase.Position;
        var dutyInfoScale = dutyInfoAddon->AtkUnitBase.Scale;

        var dutyNameContainer = dutyInfoAddon->AtkUnitBase.GetNodeById<AtkComponentNode>(4);
        if (dutyNameContainer is null) return;
        var dutyNameContainerPos = new Vector2(dutyNameContainer->X, dutyNameContainer->Y) * dutyInfoScale;

        var dutyLootButtonPos = new Vector2(236.0f, 29.0f) * dutyInfoScale;

        Position = dutyInfoPos + dutyNameContainerPos + dutyLootButtonPos;
        Scale = new Vector2(dutyInfoScale, dutyInfoScale);

        UpdateVisibility();
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        
        buttonNode.Size = Size;
    }

    private void OnTerritoryChanged(ushort territoryTypeId) {
        shouldShow = ShouldShowButton();
        UpdateVisibility();
    }

    private void UpdateVisibility() {
        if (!shouldShow) {
            IsVisible = false;
            return;
        }

        var dutyInfoAddon = Services.GameGui.GetAddonByName<AddonToDoList>("_ToDoList");
        if (!dutyInfoAddon->AtkUnitBase.IsActuallyVisible) {
            IsVisible = false;
            return;
        }

        var dutyNameContainer = dutyInfoAddon->AtkUnitBase.GetNodeById<AtkComponentNode>(4);
        if (dutyNameContainer is null || !dutyNameContainer->AtkResNode.IsActuallyVisible) {
            IsVisible = false;
            return;
        }

        IsVisible = true;
    }
    
    private static bool ShouldShowButton(ushort? territoryRow = null) {
        var territory = territoryRow ?? Services.ClientState.TerritoryType;
        if (territory is 0) return false;
        
        var territoryType = Services.DataManager.GetExcelSheet<TerritoryType>().GetRow(territory);
        var contentTypeId = territoryType.ContentFinderCondition.Value.ContentType.RowId;
        return contentTypeId is 2 or 4 or 5; // Dungeon, Trial, Raid (including Alliance)
    }
}
