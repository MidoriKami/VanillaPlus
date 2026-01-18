using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Overlay;
using VanillaPlus.Features.DutyLootPreview.Data;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview.Nodes;

public unsafe class DutyLootInDutyButtonNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    private readonly DutyLootOpenWindowButtonNode buttonNode;
    private readonly DutyLootDataLoader dataLoader;

    public Action? OnClick {
        get => buttonNode.OnClick;
        set => buttonNode.OnClick = value;
    }

    public DutyLootInDutyButtonNode(DutyLootDataLoader dataLoader) {
        this.dataLoader = dataLoader;

        buttonNode = new DutyLootOpenWindowButtonNode(dataLoader) {
            Size = new Vector2(20.0f, 20.0f),
            TextTooltip = Strings.DutyLoot_Tooltip_InDutyButton,
            IsVisible = true
        };
        buttonNode.AttachNode(this);
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

    private void UpdateVisibility() {
        var lootData = dataLoader.CurrentDutyLootData;
        if (lootData.ContentId is null && !lootData.IsLoading) {
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
}
