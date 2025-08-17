using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ListInventory;

public unsafe class AddonListInventory : NativeAddon {

    private ScrollingAreaNode<VerticalListNode>? scrollingAreaNode;
    private VerticalListNode? ListNode => scrollingAreaNode?.ContentNode;
    
    private TextInputNode? textInputNode;
    private TextNode? searchLabelNode;
    private TextDropDownNode? sortDropdownNode;

    public required AddonConfig Config { get; init; }

    protected override void OnSetup(AtkUnitBase* addon) {
        addon->SubscribeAtkArrayData(0, (int)StringArrayType.Inventory);
        addon->SubscribeAtkArrayData(1, (int)NumberArrayType.Inventory);
        
        sortDropdownNode = new TextDropDownNode {
            Size = new Vector2(150.0f, 28.0f),
            Position = ContentStartPosition + new Vector2(ContentSize.X, 0.0f) - new Vector2(150.0f, 0.0f) + new Vector2(0.0f, 1.0f),
            MaxListOptions = 6,
            Options = ["Alphabetically", "Level", "Item Level", "Rarity", "Item Id", "Item Category"],
            IsVisible = true,
            OnOptionSelected = _ => UpdateInventoryList(),
        };
        
        textInputNode = new TextInputNode {
            Size = new Vector2(ContentSize.X - sortDropdownNode.Width - 4.0f, 28.0f),
            Position = ContentStartPosition,
            IsVisible = true,
        };

        searchLabelNode = new TextNode {
            Position = new Vector2(10.0f, 6.0f),
            IsVisible = true,
            TextColor = ColorHelper.GetColor(3),
            Text = "Search . . .",
        };

        textInputNode.OnFocused += () => {
            searchLabelNode.IsVisible = false;
        };

        textInputNode.OnUnfocused += () => {
            if (textInputNode.String.ToString() is "") {
                searchLabelNode.IsVisible = true;
            }
        };

        textInputNode.OnInputReceived += _ => {
            UpdateInventoryList();
        };
        
        const float listPadding = 4.0f;
        
        scrollingAreaNode = new ScrollingAreaNode<VerticalListNode> {
            Size = ContentSize - new Vector2(0.0f, textInputNode.Height + listPadding),
            Position = ContentStartPosition + new Vector2(0.0f, textInputNode.Height + listPadding),
            ContentHeight = 1000.0f,
            IsVisible = true,
        };
        
        AttachNode(scrollingAreaNode);
        AttachNode(textInputNode);
        AttachNode(searchLabelNode, textInputNode);
        AttachNode(sortDropdownNode);

        foreach (var _ in Enumerable.Range(0, 140)) {
            var newItemNode = new InventoryItemNode { 
                Size = new Vector2(scrollingAreaNode.ContentNode.Width, 32.0f),
                IsVisible = false,
                OnHovered = OnItemHovered,
            };
            
            ListNode?.AddNode(newItemNode);
        }
        
        ListNode?.ItemSpacing = 3.0f;
        ListNode?.RecalculateLayout();
        RecalculateContentHeight();
    }

    private bool tooltipShowing;

    private void OnItemHovered(InventoryItemNode itemNode, bool isHovered) {
        if (!itemNode.IsVisible) return;
        if (itemNode.Item is null) return;
        if (!sortDropdownNode?.IsCollapsed ?? true) return;
        if (itemNode.ScreenY < scrollingAreaNode?.ScreenY) return;
        
        if (isHovered && !tooltipShowing) {
            var tooltipArgs = stackalloc AtkTooltipManager.AtkTooltipArgs[1];
            tooltipArgs->ItemArgs = new AtkTooltipManager.AtkTooltipArgs.AtkTooltipItemArgs {
                Kind = DetailKind.Item,
                ItemId = (int)itemNode.Item.Item.ItemId,
            };

            AtkStage.Instance()->TooltipManager.ShowTooltip(
                AtkTooltipManager.AtkTooltipType.Item,
                ((AtkUnitBase*)this)->Id,
                (AtkResNode*)itemNode,
                tooltipArgs
            );
            tooltipShowing = true;
        }
        else {
            AtkStage.Instance()->TooltipManager.HideTooltip((ushort)AddonId);
            tooltipShowing = false;
        }
    }

    protected override void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData) {
        UpdateInventoryList();
    }

    private void UpdateInventoryList() {
        if (ListNode?.Nodes.Count is 0) return;
        
        AtkStage.Instance()->TooltipManager.HideTooltip((ushort)AddonId);
        
        foreach (var node in ListNode?.GetNodes<InventoryItemNode>() ?? []) {
            node.IsVisible = false;
            node.Item = null;
        }

        var nodeIndex = 0;
        
        foreach (var itemInfo in GetOrderedInventoryItems()) {
            if (ListNode?.Nodes[nodeIndex] is InventoryItemNode node) {
                node.Item = itemInfo;
                node.IsVisible = true;
                nodeIndex++;
            }
        }

        ListNode?.RecalculateLayout();
        RecalculateContentHeight();
    }

    private IOrderedEnumerable<ItemInfo> GetOrderedInventoryItems() {
        IEnumerable<ItemInfo> inventoryItems = GetInventoryItems();

        if (textInputNode?.String.ToString() is { Length: > 0 } searchString) {
            inventoryItems = inventoryItems.Where(item => item.IsRegexMatch(searchString));
        }

        return sortDropdownNode?.SelectedOption switch {
            "Alphabetically" => inventoryItems.OrderBy(item => item.Name),
            "Level" => inventoryItems.OrderByDescending(item => item.Level).ThenBy(item => item.Name),
            "Item Level" => inventoryItems.OrderByDescending(item => item.ItemLevel).ThenBy(item => item.Name),
            "Rarity" => inventoryItems.OrderByDescending(item => item.Rarity).ThenBy(item => item.Name),
            "Item Id" => inventoryItems.OrderByDescending(item => item.Item.ItemId),
            "Item Category" => inventoryItems.OrderBy(item => item.UiCategory).ThenBy(item => item.Name),
            _ => inventoryItems.OrderBy(item => item.Name),
        };
    }

    protected override void OnFinalize(AtkUnitBase* addon) {
        addon->UnsubscribeAtkArrayData(0, (int)StringArrayType.Inventory);
        addon->UnsubscribeAtkArrayData(1, (int)NumberArrayType.Inventory);

        System.NativeController.DisposeNode(ref sortDropdownNode);
        System.NativeController.DisposeNode(ref scrollingAreaNode);
        System.NativeController.DisposeNode(ref searchLabelNode);
        System.NativeController.DisposeNode(ref textInputNode);
    }

    private static List<ItemInfo> GetInventoryItems() {
        List<InventoryType> inventories = [ InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4 ];
        List<InventoryItem> items = [];

        foreach (var inventory in inventories) {
            var container = InventoryManager.Instance()->GetInventoryContainer(inventory);

            for (var index = 0; index < container->Size; ++index) {
                ref var item = ref container->Items[index];
                if (item.ItemId is 0) continue;
                
                items.Add(item);
            }
        }

        List<ItemInfo> itemInfos = [];
        itemInfos.AddRange(from itemGroups in items.GroupBy(item => item.ItemId)
                           where itemGroups.Key is not 0
                           let item = itemGroups.First()
                           let itemCount = itemGroups.Sum(duplicateItem => duplicateItem.Quantity)
                           select new ItemInfo {
                               Item = item, ItemCount = itemCount,
                           });

        return itemInfos;
    }
    
    protected override void OnHide(AtkUnitBase* addon) {
        Config.WindowPosition = Position;
        Config.Save();
    }

    private void RecalculateContentHeight() {
        scrollingAreaNode?.ContentHeight = ListNode?.Nodes.Sum(item => item.IsVisible ? item.Height + scrollingAreaNode.ContentNode.ItemSpacing : 0.0f ) ?? 1000.0f;
    }
}
