using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.NativeElements.Nodes;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.ListInventory;

public class InventoryItemNode : ListItemNode<ItemStack> {
    public override float ItemHeight => 32.0f;
    
    private readonly IconWithCountNode iconNode;
    private readonly TextNode itemNameTextNode;
    private readonly TextNode levelTextNode;
    private readonly TextNode itemLevelTextNode;

    public InventoryItemNode() {
        EnableSelection = false;
        
        iconNode = new IconWithCountNode();
        iconNode.AttachNode(this);

        iconNode.CollisionNode.ShowClickableCursor = true;

        itemNameTextNode = new TextNode {
            AlignmentType = AlignmentType.Left,
            TextFlags = TextFlags.Ellipsis,
        };
        itemNameTextNode.AttachNode(this);

        levelTextNode = new TextNode {
            AlignmentType = AlignmentType.Left,
        };
        levelTextNode.AttachNode(this);

        itemLevelTextNode = new TextNode {
            AlignmentType = AlignmentType.Left,
        };
        itemLevelTextNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        iconNode.Size = new Vector2(Height, Height);
        iconNode.Position = Vector2.Zero;

        itemLevelTextNode.Size = new Vector2(64.0f, Height);
        itemLevelTextNode.Position = new Vector2(Width - itemLevelTextNode.Width, 0.0f);

        levelTextNode.Size = new Vector2(64.0f, Height);
        levelTextNode.Position = new Vector2(Width - levelTextNode.Width - itemLevelTextNode.Width, 0.0f);

        itemNameTextNode.Size = new Vector2(Width - iconNode.Width - itemLevelTextNode.Width - levelTextNode.Width - 8.0f, Height);
        itemNameTextNode.Position = new Vector2(iconNode.Width + 4.0f, 0.0f);
    }

    protected override void SetNodeData(ItemStack itemData) {
        if (!Services.DataManager.GetExcelSheet<Item>().TryGetRow(itemData.Item.ItemId, out var luminaData)) return;
        
        iconNode.IconId = luminaData.Icon;
        itemNameTextNode.SeString = luminaData.Name;
        iconNode.Count = itemData.Quantity;
        iconNode.CollisionNode.InventoryItemTooltip = new InventoryItemTooltip(itemData.Item.Container, itemData.Item.Slot);

        if (luminaData.LevelEquip > 1) {
            levelTextNode.String = $"Lv. {luminaData.LevelEquip,3}";
        }
        else {
            levelTextNode.String = string.Empty;
        }

        if (luminaData.LevelItem.RowId > 1) {
            itemLevelTextNode.String = $"iLvl. {luminaData.LevelItem.RowId,3}";
        }
        else {
            itemLevelTextNode.String = string.Empty;
        }
    }
}
