using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using VanillaPlus.NativeElements.Nodes;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public unsafe class LootItemNode : SimpleComponentNode {

    private readonly NineGridNode hoveredBackgroundNode;
    private readonly IconWithCountNode iconNode;
    private readonly TextNode itemNameTextNode;
    
    public LootItemNode() {
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

        itemNameTextNode = new TextNode {
            TextFlags = TextFlags.Ellipsis,
            AlignmentType = AlignmentType.Left,
        };
        itemNameTextNode.AttachNode(this);
        
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
    }
    
    public bool IsHovered {
        get => hoveredBackgroundNode.IsVisible;
        set => hoveredBackgroundNode.IsVisible = value;
    }

    public required LootedItemInfo Item {
        get;
        set {
            field = value;

            iconNode.IconId = value.IconId;
            itemNameTextNode.SeString = value.Name;
            iconNode.Count = value.Quantity;
        }
    }
    
    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        iconNode.Size = new Vector2(Height, Height);
        iconNode.Position = Vector2.Zero;

        itemNameTextNode.Size = new Vector2(Width - iconNode.Width - 4.0f, Height);
        itemNameTextNode.Position = new Vector2(iconNode.Width + 4.0f, 0.0f);
        
        hoveredBackgroundNode.Size = Size;
    }
}
