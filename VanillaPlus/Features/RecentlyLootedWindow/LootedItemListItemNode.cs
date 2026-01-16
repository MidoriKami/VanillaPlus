using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using VanillaPlus.NativeElements.Nodes;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public class LootedItemListItemNode : ListItemNode<LootedItemInfo> {
    public override float ItemHeight => 32.0f;
    
    private readonly IconWithCountNode iconNode;
    private readonly TextNode itemNameTextNode;
    
    public LootedItemListItemNode() {
        EnableHighlight = false;
        EnableSelection = false;
        DisableCollisionNode = true;
        
        iconNode = new IconWithCountNode {
            ShowClickableCursor = true,
        };
        iconNode.AttachNode(this);

        itemNameTextNode = new TextNode {
            TextFlags = TextFlags.Ellipsis,
            AlignmentType = AlignmentType.Left,
        };
        itemNameTextNode.AttachNode(this);
    }
    
    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        iconNode.Size = new Vector2(Height, Height);
        iconNode.Position = Vector2.Zero;

        itemNameTextNode.Size = new Vector2(Width - iconNode.Width - 4.0f, Height);
        itemNameTextNode.Position = new Vector2(iconNode.Width + 4.0f, 0.0f);
    }

    protected override void SetNodeData(LootedItemInfo itemData) {
        iconNode.IconId = itemData.IconId;
        itemNameTextNode.SeString = itemData.Name;
        iconNode.Count = itemData.Quantity;
        iconNode.CollisionNode.ItemTooltip = itemData.ItemId;
    }
}
