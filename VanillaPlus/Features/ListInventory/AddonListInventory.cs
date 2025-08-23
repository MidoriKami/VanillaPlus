using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ListInventory;

public unsafe class AddonListInventory : NativeAddon {

    private ScrollingAreaNode<VerticalListNode>? scrollingAreaNode;
    private VerticalListNode? ListNode => scrollingAreaNode?.ContentNode;
    
    private TextInputNode? textInputNode;
    private TextNode? searchLabelNode;
    private TextDropDownNode? sortDropdownNode;
    
    private VerticalListNode? mainContainerNode;
    private HorizontalFlexNode? searchContainerNode;
    private HorizontalListNode? widgetsContainerNode;
    private CircleButtonNode? reverseButtonNode;

    public required AddonConfig Config { get; init; }

    private bool reverseSort;

    protected override void OnSetup(AtkUnitBase* addon) {
        addon->SubscribeAtkArrayData(0, (int)StringArrayType.Inventory);
        addon->SubscribeAtkArrayData(1, (int)NumberArrayType.Inventory);

        const float dropDownWidth = 175.0f;

        mainContainerNode = new VerticalListNode {
            Position = ContentStartPosition,
            Size = ContentSize,
            IsVisible = true,
        };

        searchContainerNode = new HorizontalFlexNode {
            Size = new Vector2(ContentSize.X, 28.0f),
            AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
            IsVisible = true,
        };

        widgetsContainerNode = new HorizontalListNode {
            Size = new Vector2(ContentSize.X, 28.0f),
            Alignment = HorizontalListAnchor.Right,
            IsVisible = true,
        };
        
        sortDropdownNode = new TextDropDownNode {
            Size = new Vector2(dropDownWidth, 28.0f),
            MaxListOptions = 7,
            Options = ["Alphabetically", "Quantity", "Level", "Item Level", "Rarity", "Item Id", "Item Category"],
            IsVisible = true,
            OnOptionSelected = _ => UpdateInventoryList(),
        };

        reverseButtonNode = new CircleButtonNode {
            Size = new Vector2(28.0f, 28.0f),
            Icon = ButtonIcon.Sort,
            IsVisible = true,
            OnClick = () => {
                reverseSort = !reverseSort;
                UpdateInventoryList();
            },
            Tooltip = "Reverse Sort Direction",
        };
        
        textInputNode = new TextInputNode {
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
            Size = ContentSize - new Vector2(0.0f, searchContainerNode.Height + widgetsContainerNode.Height + listPadding),
            Position = new Vector2(0.0f, listPadding),
            ContentHeight = 1000.0f,
            IsVisible = true,
        };
        
        AttachNode(mainContainerNode);
        mainContainerNode.AddNode(searchContainerNode);
        searchContainerNode.AddNode(textInputNode);
        mainContainerNode.AddNode(widgetsContainerNode);
        widgetsContainerNode.AddNode(reverseButtonNode);

        sortDropdownNode.Width = widgetsContainerNode.AreaRemaining;
        widgetsContainerNode.AddNode(sortDropdownNode);
        
        AttachNode(searchLabelNode, textInputNode);
        
        mainContainerNode.AddDummy(4.0f);
        mainContainerNode.AddNode(scrollingAreaNode);

        if (ListNode is not null) {
            ListNode.ItemSpacing = 3.0f;
            ListNode.RecalculateLayout();
        }

        RecalculateContentHeight();
    }

    private bool tooltipShowing;

    private void OnItemHovered(InventoryItemNode itemNode, bool isHovered) {
        if (!itemNode.IsVisible) return;
        if (itemNode.Item is null) return;
        if (!sortDropdownNode?.IsCollapsed ?? true) return;
        
        if (isHovered && !tooltipShowing) {
            var tooltipArgs = stackalloc AtkTooltipManager.AtkTooltipArgs[1];
            tooltipArgs->ItemArgs = new AtkTooltipManager.AtkTooltipArgs.AtkTooltipItemArgs {
                Kind = DetailKind.InventoryItem,
                InventoryType = itemNode.Item.Item.Container,
                Slot = itemNode.Item.Item.Slot,
                BuyQuantity = -1,
                Flag1 = 0,
            };

            AtkStage.Instance()->TooltipManager.ShowTooltip(
                AtkTooltipManager.AtkTooltipType.Item,
                ((AtkUnitBase*)this)->Id,
                (AtkResNode*)itemNode.CollisionNode.Node,
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
        if (ListNode is null) return;
        if (scrollingAreaNode is null) return;
        
        AtkStage.Instance()->TooltipManager.HideTooltip((ushort)AddonId);

        ListNode.SyncWithListData(GetInventoryItems(), GetDataFromNode, CreateInventoryNode);
        ListNode.ReorderNodes(Comparison);
        
        RecalculateContentHeight();
    }

    private InventoryItemNode CreateInventoryNode(ItemInfo data) => new() {
        Size = new Vector2(scrollingAreaNode!.ContentNode.Width, 32.0f), 
        Item = data, 
        IsVisible = true, 
        OnHovered = OnItemHovered,
    };

    private static ItemInfo? GetDataFromNode(InventoryItemNode node)
        => node.Item;

    private int Comparison(NodeBase x, NodeBase y) {
        if (x is not InventoryItemNode left || y is not InventoryItemNode right) return 0;

        var leftItem = left.Item;
        var rightItem = right.Item;
        if (leftItem is null || rightItem is null) return 0;

        // Note: Compares in opposite direction to be descending instead of ascending, except for alphabetically

        var result = sortDropdownNode?.SelectedOption switch {
            "Alphabetically" => string.CompareOrdinal(leftItem.Name, rightItem.Name),
            "Level" => rightItem.Level.CompareTo(leftItem.Level),
            "Item Level" => rightItem.ItemLevel.CompareTo(leftItem.ItemLevel),
            "Rarity" => rightItem.Rarity.CompareTo(leftItem.Rarity),
            "Item Id" => rightItem.Item.ItemId.CompareTo(leftItem.Item.ItemId),
            "Item Category" => rightItem.UiCategory.CompareTo(leftItem.UiCategory),
            "Quantity" => rightItem.ItemCount.CompareTo(leftItem.ItemCount),
            _ => string.CompareOrdinal(leftItem.Name, rightItem.Name),
        };

        var reverseModifier = reverseSort ? -1 : 1;
        
        return ( result is 0 ? string.CompareOrdinal(leftItem.Name, rightItem.Name) : result ) * reverseModifier;
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
        if (scrollingAreaNode is null || ListNode is null) return;
        
        scrollingAreaNode.ContentHeight = ListNode.Nodes.Sum(item => item.IsVisible ? item.Height + scrollingAreaNode.ContentNode.ItemSpacing : 0.0f );
    }
}
