using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using VanillaPlus.NativeElements.Nodes;

namespace VanillaPlus.Features.DutyLootPreview.Nodes;

public unsafe class DutyLootNode : ListItemNode<DutyLootItem> {
    public override float ItemHeight => 36.0f;
    
    private readonly IconWithCountNode iconNode;
    private readonly TextNode itemNameTextNode;
    private readonly SimpleImageNode favoriteStarNode;
    private readonly SimpleImageNode infoIconNode;
    private readonly SimpleImageNode checkmarkIconNode;

    public Action<DutyLootItem>? OnLeftClick;
    public Action<DutyLootItem>? OnRightClick;
    
    public DutyLootNode() {
        iconNode = new IconWithCountNode();
        iconNode.AttachNode(this);

        favoriteStarNode = new SimpleImageNode {
            TextureCoordinates = new Vector2(96, 0),
            TextureSize = new Vector2(20, 20),
            TexturePath = "ui/uld/MinionNoteBook.tex",
            Size = new Vector2(20, 20),
            IsVisible = false,
        };
        favoriteStarNode.AttachNode(this);

        itemNameTextNode = new TextNode {
            TextFlags = TextFlags.Ellipsis,
            AlignmentType = AlignmentType.Left,
        };
        itemNameTextNode.AttachNode(this);

        infoIconNode = new SimpleImageNode {
            TextureCoordinates = new Vector2(112, 84),
            TextureSize = new Vector2(28, 28),
            TexturePath = "ui/uld/CircleButtons.tex",
            WrapMode = WrapMode.Stretch,
        };
        infoIconNode.AttachNode(this);

        checkmarkIconNode = new SimpleImageNode {
            TextureCoordinates = new Vector2(60, 28),
            TextureSize = new Vector2(28, 24),
            TexturePath = "ui/uld/RecipeNoteBook.tex",
            IsVisible = false,
        };
        checkmarkIconNode.AttachNode(this);

        CollisionNode.AddEvent(AtkEventType.MouseClick, (_, _, _, _, atkEventData) => {
            if (ItemData is null) return;

            if (atkEventData->IsLeftClick) {
                OnLeftClick?.Invoke(ItemData);
            }
            else if (atkEventData->IsRightClick) {
                OnRightClick?.Invoke(ItemData);
            }
        });
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        iconNode.Size = new Vector2(Height, Height);
        iconNode.Position = Vector2.Zero;

        // Scale star proportionally (original: 20x20 star on 44x44 icon)
        var starSize = iconNode.Height * (20f / 44f);
        favoriteStarNode.Size = new Vector2(starSize, starSize);

        // Position in top-right corner, slightly above icon edge
        favoriteStarNode.Position = new Vector2(iconNode.Width - favoriteStarNode.Width, -2);

        var infoSize = Size.Y * 0.6f;
        infoIconNode.Size = new Vector2(infoSize, infoSize);
        infoIconNode.Position = new Vector2(Width - infoSize, Height / 2 - infoSize / 2);

        checkmarkIconNode.Size = new Vector2(28, 24);
        checkmarkIconNode.Position = iconNode.Size - checkmarkIconNode.Size * 0.8f;

        itemNameTextNode.Size = new Vector2(Width - iconNode.Width - infoSize - 12.0f, Height);
        itemNameTextNode.Position = new Vector2(iconNode.Width + 4.0f, 0.0f);
    }
    
    public bool IsFavorite {
        get => favoriteStarNode.IsVisible;
        set => favoriteStarNode.IsVisible = value;
    }
    
    protected override void SetNodeData(DutyLootItem itemData) {
        iconNode.IconId = itemData.IconId;
        itemNameTextNode.SeString = itemData.Name;
        iconNode.Count = 1;
        infoIconNode.TextTooltip = string.Join("\n", itemData.Sources);
        checkmarkIconNode.IsVisible = itemData.IsUnlocked;
        CollisionNode.ItemTooltip = itemData.ItemId;
    }
}
