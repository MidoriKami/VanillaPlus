﻿using System.Numerics;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Game.Text;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using AtkItemTooltipArgs = FFXIVClientStructs.FFXIV.Component.GUI.AtkTooltipManager.AtkTooltipArgs.AtkTooltipItemArgs;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public unsafe class LootItemNode : SimpleComponentNode {

    private readonly NineGridNode hoveredBackgroundNode;
    private readonly IconImageNode iconImageNode;
    private readonly TextNode itemNameTextNode;
    private readonly TextNode itemQuantityTextNode;
    
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

        itemQuantityTextNode = new TextNode {
            NodeId = 4,
            Size = new Vector2(32.0f, 8.0f),
            Position = new Vector2(0.0f, 28.0f), 
            IsVisible = true,
            AlignmentType = AlignmentType.BottomRight,
            TextFlags = TextFlags.Edge,
            FontSize = 12,
        };
        System.NativeController.AttachNode(itemQuantityTextNode, this);

        itemNameTextNode = new TextNode {
            NodeId = 5,
            TextFlags = TextFlags.Ellipsis,
            AlignmentType = AlignmentType.Left,
            IsVisible = true,
        };
        System.NativeController.AttachNode(itemNameTextNode, this);
        
        CollisionNode.AddEvent(AddonEventType.MouseOver, _ => {
            IsHovered = true;
            
            var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode((AtkResNode*)InternalComponentNode);
            if (addon is not null) {
                
                var tooltipArgs = new AtkTooltipManager.AtkTooltipArgs();
                tooltipArgs.Ctor();
                tooltipArgs.ItemArgs = new AtkItemTooltipArgs {
                    Kind = DetailKind.ItemId,
                    ItemId = (int) Item!.Event.Item.BaseItemId,
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

    public required IndexedItemEvent Item {
        get;
        set {
            field = value;

            var (itemBaseId, itemKind) = ItemUtil.GetBaseId(value.Event.Item.ItemId);
            switch (itemKind) {
                case ItemKind.Normal:
                case ItemKind.Collectible:
                case ItemKind.Hq:
                    var item = Services.DataManager.GetExcelSheet<Item>().GetRow(itemBaseId);
                    iconImageNode.IconId = item.Icon;
                    itemNameTextNode.ReadOnlySeString = GetItemName(itemBaseId);
                    break;

                case ItemKind.EventItem:
                    var eventItem = Services.DataManager.GetExcelSheet<EventItem>().GetRow(itemBaseId);
                    iconImageNode.IconId = eventItem.Icon;
                    itemNameTextNode.String = eventItem.Name.ToString();
                    break;

                default:
                    iconImageNode.IconId = 60071;
                    itemNameTextNode.String = $"Unknown Item Type, ID: {value.Event.Item.ItemId}";
                    break;
            }

            switch (value.Event) {
                case InventoryItemAddedArgs addedArgs:
                    if (addedArgs.Item.Quantity > 1) {
                        itemQuantityTextNode.String = addedArgs.Item.Quantity.ToString();
                    }
                    break;
                
                case InventoryItemChangedArgs changedArgs:
                    var quantity = changedArgs.Item.Quantity - changedArgs.OldItemState.Quantity;

                    if (quantity > 1) {
                        itemQuantityTextNode.String = quantity.ToString();
                    }
                    break;
            }
        }
    }
    
    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        itemNameTextNode.Size = Size - new Vector2(iconImageNode.X + iconImageNode.Width + 4.0f, 0.0f);
        itemNameTextNode.Position = new Vector2(iconImageNode.X + iconImageNode.Width + 4.0f, 0.0f);
        
        hoveredBackgroundNode.Size = Size;
    }

    private static ReadOnlySeString GetItemName(uint itemId) {
        var itemName = ItemUtil.IsEventItem(itemId)
                           ? Services.DataManager.GetExcelSheet<EventItem>().TryGetRow(itemId, out var eventItem) ? eventItem.Name : default
                           : Services.DataManager.GetExcelSheet<Item>().TryGetRow(ItemUtil.GetBaseId(itemId).ItemId, out var item) ? item.Name : default;

        if (ItemUtil.IsHighQuality(itemId))
            itemName += " " + SeIconChar.HighQuality.ToIconString();
        else if (ItemUtil.IsCollectible(itemId))
            itemName += " " + SeIconChar.Collectible.ToIconString();

        return new Lumina.Text.SeStringBuilder()
            .PushColorType(ItemUtil.GetItemRarityColorType(itemId))
            // .PushEdgeColorType(ItemUtil.GetItemRarityColorType(itemId, true))
            .Append(itemName)
            // .PopEdgeColorType()
            .PopColorType()
            .ToReadOnlySeString();
    }
}
