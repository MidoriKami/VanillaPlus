using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.NativeElements.Nodes;

namespace VanillaPlus.Features.DutyLootPreview;

public unsafe class DutyLootNode : SimpleComponentNode {
    private readonly NineGridNode hoveredBackgroundNode;
    private readonly IconWithCountNode iconNode;
    private readonly TextNode itemNameTextNode;
    private readonly SimpleImageNode favoriteStarNode;
    private readonly SimpleImageNode infoIconNode;

    public Action<DutyLootItem>? OnLeftClick;
    public Action<DutyLootItem>? OnRightClick;
    
    public DutyLootNode() {
        hoveredBackgroundNode = new SimpleNineGridNode {
            TexturePath = "ui/uld/ListItemA.tex",
            TextureCoordinates = new Vector2(0.0f, 22.0f),
            TextureSize = new Vector2(64.0f, 22.0f),
            TopOffset = 6,
            BottomOffset = 6,
            LeftOffset = 16,
            RightOffset = 1,
            IsVisible = false,
        };
        hoveredBackgroundNode.AttachNode(this);

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

        CollisionNode.AddEvent(AtkEventType.MouseOver, () => {
            IsHovered = true;

            if (Item is null) return;
            AtkResNode* node = CollisionNode;
            node->ShowItemTooltip(Item.ItemId);
        });
        
        CollisionNode.AddEvent(AtkEventType.MouseOut, () => {
            IsHovered = false;
            CollisionNode.HideTooltip();
        });

        CollisionNode.AddEvent(AtkEventType.MouseClick, (_, _, _, _, atkEventData) => {
            if (Item is null) return;

            if (atkEventData->IsLeftClick()) {
                OnLeftClick?.Invoke(Item);
            }
            else if (atkEventData->IsRightClick()) {
                OnRightClick?.Invoke(Item);
            }
        });
    }
    
    public bool IsHovered {
        get => hoveredBackgroundNode.IsVisible;
        set => hoveredBackgroundNode.IsVisible = value;
    }

    public bool IsFavorite {
        get => favoriteStarNode.IsVisible;
        set => favoriteStarNode.IsVisible = value;
    }

    public required DutyLootItem Item {
        get;
        set {
            field = value;

            iconNode.IconId = value.IconId;
            itemNameTextNode.SeString = value.Name;
            iconNode.Count = 1; // value.Quantity;
            infoIconNode.Tooltip = string.Join("\n", value.Sources);
        }
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

        itemNameTextNode.Size = new Vector2(Width - iconNode.Width - infoSize - 12.0f, Height);
        itemNameTextNode.Position = new Vector2(iconNode.Width + 4.0f, 0.0f);

        hoveredBackgroundNode.Size = Size;
    }
}
