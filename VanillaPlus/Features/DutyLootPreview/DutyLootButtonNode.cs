using System;
using System.Numerics;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;

namespace VanillaPlus.Features.DutyLootPreview;

public unsafe class DutyLootButtonNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.AboveUserInterface;

    private readonly TextureButtonNode buttonNode;

    public Action? OnClick {
        get => buttonNode.OnClick;
        set => buttonNode.OnClick = value;
    }

    public DutyLootButtonNode() {
        buttonNode = new TextureButtonNode {
            TexturePath = "ui/uld/Inventory.tex",
            TextureCoordinates = new Vector2(90.0f, 125.0f),
            TextureSize = new Vector2(32.0f, 32.0f),
            Size = new Vector2(20.0f, 20.0f),
            TooltipString = "[VanillaPlus] Open Duty Loot Preview Window",
        };
        buttonNode.AttachNode(this);

        CollisionNode.IsVisible = true;

        Size = new Vector2(20.0f, 20.0f);
    }
}
