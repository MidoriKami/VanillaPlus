using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using VanillaPlus.NativeElements.Nodes;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public class LootItemNode : SelectableNode {

    private readonly IconWithCountNode iconNode;
    private readonly TextNode itemNameTextNode;
    
    public LootItemNode() {
        iconNode = new IconWithCountNode();
        iconNode.AttachNode(this);

        itemNameTextNode = new TextNode {
            TextFlags = TextFlags.Ellipsis,
            AlignmentType = AlignmentType.Left,
        };
        itemNameTextNode.AttachNode(this);
    }

    public required LootedItemInfo Item {
        get;
        set {
            field = value;

            iconNode.IconId = value.IconId;
            itemNameTextNode.SeString = value.Name;
            iconNode.Count = value.Quantity;
            CollisionNode.ItemTooltip = Item.ItemId;
        }
    }
    
    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        iconNode.Size = new Vector2(Height, Height);
        iconNode.Position = Vector2.Zero;

        itemNameTextNode.Size = new Vector2(Width - iconNode.Width - 4.0f, Height);
        itemNameTextNode.Position = new Vector2(iconNode.Width + 4.0f, 0.0f);
    }
}
