using System.Numerics;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.Extensions;
using AtkItemTooltipArgs = FFXIVClientStructs.FFXIV.Component.GUI.AtkTooltipManager.AtkTooltipArgs.AtkTooltipItemArgs;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public unsafe class LootItemNode : SimpleComponentNode {

    private readonly NineGridNode hoveredBackgroundNode;
    private readonly IconImageNode iconImageNode;
    private readonly TextNode itemNameNode;
    
    public LootItemNode() {
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
        
        iconImageNode = new IconImageNode {
            NodeId = 3,
            Size = new Vector2(32.0f, 32.0f),
            Position = new Vector2(2.0f, 2.0f),
            IconId = 19,
            IsVisible = true,
        };
        System.NativeController.AttachNode(iconImageNode, this);

        itemNameNode = new TextNode {
            NodeId = 4,
            TextFlags = TextFlags.Ellipsis,
            AlignmentType = AlignmentType.Left,
            IsVisible = true,
        };
        System.NativeController.AttachNode(itemNameNode, this);
        
        CollisionNode.AddEvent(AddonEventType.MouseOver, _ => {
            IsHovered = true;
            
            var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode((AtkResNode*)InternalComponentNode);
            if (addon is not null) {
                
                var tooltipArgs = new AtkTooltipManager.AtkTooltipArgs();
                tooltipArgs.Ctor();
                tooltipArgs.ItemArgs = new AtkItemTooltipArgs {
                    Kind = DetailKind.ItemId,
                    ItemId = (int) Item.BaseItemId,
                };
                
                AtkStage.Instance()->TooltipManager.ShowTooltip(
                    AtkTooltipManager.AtkTooltipType.Item,
                    addon->Id,
                    (AtkResNode*)InternalComponentNode,
                    &tooltipArgs);
            }
        });
        
        CollisionNode.AddEvent(AddonEventType.MouseOut, _ => {
            IsHovered = false;
            HideTooltip();
        });
    }
    
    public bool IsHovered {
        get => hoveredBackgroundNode.IsVisible;
        set => hoveredBackgroundNode.IsVisible = value;
    }

    private GameInventoryItem Item {
        get;
        set {
            field = value;

            var (itemBaseId, itemKind) = ItemUtil.GetBaseId(value.ItemId);
            switch (itemKind) {
                case ItemKind.Normal:
                case ItemKind.Collectible:
                case ItemKind.Hq:
                    var item = Services.DataManager.GetExcelSheet<Item>().GetRow(itemBaseId);
                    iconImageNode.IconId = item.Icon;
                    itemNameNode.Text = item.Name.ToString();
                    itemNameNode.TextColor = item.RarityColor();
                    break;

                case ItemKind.EventItem:
                    var eventItem = Services.DataManager.GetExcelSheet<EventItem>().GetRow(itemBaseId);
                    iconImageNode.IconId = eventItem.Icon;
                    itemNameNode.Text = eventItem.Name.ToString();
                    break;

                default:
                    iconImageNode.IconId = 60071;
                    itemNameNode.Text = $"Unknown Item Type, ID: {value.ItemId}";
                    break;
            }
        }
    }

    public void SetItem(InventoryItemAddedArgs item) {
        Item = item.Item;

        if (item.Item.Quantity > 1) {
            itemNameNode.Text += $" x{item.Item.Quantity}";
        }
    }

    public void SetItem(InventoryItemChangedArgs item) {
        var quantity = item.Item.Quantity - item.OldItemState.Quantity;

        Item = item.Item;
        itemNameNode.Text += $" x{quantity}";
    }
    
    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        itemNameNode.Size = Size - new Vector2(iconImageNode.X + iconImageNode.Width + 4.0f, 0.0f);
        itemNameNode.Position = new Vector2(iconImageNode.X + iconImageNode.Width + 4.0f, 0.0f);
        
        hoveredBackgroundNode.Size = Size;
    }
}
