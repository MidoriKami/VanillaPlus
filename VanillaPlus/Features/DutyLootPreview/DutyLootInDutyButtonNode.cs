using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;

namespace VanillaPlus.Features.DutyLootPreview;

public unsafe class DutyLootInDutyButtonNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.AboveUserInterface;

    private readonly TextureButtonNode buttonNode;

    public Action? OnClick {
        get => buttonNode.OnClick;
        set => buttonNode.OnClick = value;
    }

    /// <summary>
    /// If false, never show this button. Otherwise, show it if _ToDoList is visible.
    /// </summary>
    public bool TryShow {
        get;
        set {
            field = value;
            Services.Framework.RunOnFrameworkThread(UpdateVisibility);
        }
    }

    public DutyLootInDutyButtonNode() {
        buttonNode = new TextureButtonNode {
            TexturePath = "ui/uld/Inventory.tex",
            TextureCoordinates = new Vector2(90.0f, 125.0f),
            TextureSize = new Vector2(32.0f, 32.0f),
            Size = new Vector2(20.0f, 20.0f),
            TooltipString = "[VanillaPlus] Open Duty Loot Preview Window",
        };
        buttonNode.AttachNode(this);
    }

    public override void Update() {
        base.Update();

        var dutyInfoAddon = Services.GameGui.GetAddonByName<AddonToDoList>("_ToDoList");
        var dutyInfoPos = dutyInfoAddon->AtkUnitBase.Position();
        var dutyInfoScale = dutyInfoAddon->AtkUnitBase.Scale;

        var dutyNameContainer = dutyInfoAddon->AtkUnitBase.GetNodeById<AtkComponentNode>(4);
        if (dutyNameContainer is null) return;
        var dutyNameContainerPos = new Vector2(dutyNameContainer->X, dutyNameContainer->Y) * dutyInfoScale;

        var dutyLootButtonPos = new Vector2(236f, 29f) * dutyInfoScale;

        Position = dutyInfoPos
            + dutyNameContainerPos
            + dutyLootButtonPos;

        Scale = new Vector2(dutyInfoScale, dutyInfoScale);

        UpdateVisibility();
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        
        buttonNode.Size = Size;
    }

    private void UpdateVisibility() {
        if (!TryShow) {
            IsVisible = false;
            return;
        }

        var dutyInfoAddon = Services.GameGui.GetAddonByName<AddonToDoList>("_ToDoList");
        if (!dutyInfoAddon->AtkUnitBase.IsActuallyVisible()) {
            IsVisible = false;
            return;
        }

        var dutyNameContainer = dutyInfoAddon->AtkUnitBase.GetNodeById<AtkComponentNode>(4);
        if (dutyNameContainer is null || !dutyNameContainer->AtkResNode.IsActuallyVisible()) {
            IsVisible = false;
            return;
        }

        IsVisible = true;
    }
}
