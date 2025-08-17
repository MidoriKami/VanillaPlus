using System;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.ListInventory;

public class InventoryItemNode : SimpleComponentNode {
    
    private readonly NineGridNode hoveredBackgroundNode;
    private readonly IconImageNode itemIconImageNode;
    private readonly TextNode itemCountTextNode;
    private readonly TextNode itemNameTextNode;
    private readonly TextNode levelTextNode;
    private readonly TextNode itemLevelTextNode;

    public InventoryItemNode() {
        hoveredBackgroundNode = new SimpleNineGridNode {
            NodeId = 2,
            TexturePath = "ui/uld/ListItemA.tex",
            TextureCoordinates = new Vector2(0.0f, 22.0f),
            TextureSize = new Vector2(64.0f, 22.0f),
            TopOffset = 6,
            BottomOffset = 6,
            LeftOffset = 16,
            RightOffset = 1,
            IsVisible = false,
        };
        System.NativeController.AttachNode(hoveredBackgroundNode, this);

        itemIconImageNode = new IconImageNode {
            NodeId = 3,
            IsVisible = true,
        };
        System.NativeController.AttachNode(itemIconImageNode, this);

        itemCountTextNode = new TextNode {
            NodeId = 4,
            IsVisible = true,
            AlignmentType = AlignmentType.BottomRight,
            TextFlags = TextFlags.Edge,
            FontSize = 12,
        };
        System.NativeController.AttachNode(itemCountTextNode, this);

        itemNameTextNode = new TextNode {
            NodeId = 5,
            IsVisible = true,
            AlignmentType = AlignmentType.Left,
            TextFlags = TextFlags.Ellipsis,
        };
        System.NativeController.AttachNode(itemNameTextNode, this);
        
        levelTextNode = new TextNode {
            NodeId = 6,
            IsVisible = true,
            AlignmentType = AlignmentType.Left,
        };
        System.NativeController.AttachNode(levelTextNode, this);

        itemLevelTextNode = new TextNode {
            NodeId = 7,
            IsVisible = true,
            AlignmentType = AlignmentType.Left,
        };
        System.NativeController.AttachNode(itemLevelTextNode, this);
        
        CollisionNode.AddEvent(AddonEventType.MouseOver, _ => {
            if (IsVisible) {
                IsHovered = true;
                OnHovered?.Invoke(this, true);
            }
        });
        
        CollisionNode.AddEvent(AddonEventType.MouseOut, _ => {
            if (IsVisible) {
                OnHovered?.Invoke(this, false);
            }
            IsHovered = false;
        });
    }
    
    public Action<InventoryItemNode, bool>? OnHovered { get; set; }
    
    public bool IsHovered {
        get => hoveredBackgroundNode.IsVisible;
        private set => hoveredBackgroundNode.IsVisible = value;
    }

    public ItemInfo? Item {
        get;
        set {
            if (value is null) {
                field = null;
                return;
            }
            
            field = value;
            
            itemIconImageNode.IconId = value.IconId;
            itemNameTextNode.Text = value.Name;
            itemNameTextNode.TextColor = value.RarityColor;

            if (value.ItemCount > 1) {
                itemCountTextNode.Text = value.ItemCount.ToString();
            }
            else {
                itemCountTextNode.Text = string.Empty;
            }

            if (value.Level > 1) {
                levelTextNode.Text = $"Lv. {value.Level, 3}";
            }
            else {
                levelTextNode.Text = string.Empty;
            }
            
            if (value.ItemLevel > 1) {
                itemLevelTextNode.Text = $"iLvl. {value.ItemLevel, 3}";
            }
            else {
                itemLevelTextNode.Text = string.Empty;
            }
        }
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        itemIconImageNode.Size = new Vector2(Height, Height);
        itemIconImageNode.Position = new Vector2(0.0f, 0.0f);

        itemCountTextNode.Size = new Vector2(32.0f, Height / 4.0f );
        itemCountTextNode.Position = new Vector2(0.0f, Height * 3.0f / 4.0f + 4.0f);

        itemLevelTextNode.Size = new Vector2(64.0f, Height);
        itemLevelTextNode.Position = new Vector2(Width - itemLevelTextNode.Width, 0.0f);
        
        levelTextNode.Size = new Vector2(64.0f, Height);
        levelTextNode.Position = new Vector2(Width - levelTextNode.Width - itemLevelTextNode.Width, 0.0f);

        itemNameTextNode.Size = new Vector2(Width - itemIconImageNode.Width - itemLevelTextNode.Width - levelTextNode.Width - 8.0f, Height);
        itemNameTextNode.Position = new Vector2(itemIconImageNode.Width + 4.0f, 0.0f);

        hoveredBackgroundNode.Size = Size + new Vector2(6.0f, 6.0f);
        hoveredBackgroundNode.Position = new Vector2(-3.0f, -3.0f);
    }
}
