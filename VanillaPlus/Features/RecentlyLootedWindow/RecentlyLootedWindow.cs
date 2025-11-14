using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Addons;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public unsafe class RecentlyLootedWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Recently Looted Items Window",
        Description = "Adds a window that shows a scrollable list of all items that you have looted this session.\n\n" +
                      "Can only show items looted after this feature is enabled.",
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Limit tracking to standard inventories, and armory"),
            new ChangeLogInfo(3, "Displays item quantity as text over icon instead of appended to the end of name"),
        ],
    };

    private NodeListAddon? addonRecentlyLooted;

    private bool enableTracking;
    private List<LootedItemInfo>? items;
    private bool updateRequested;

    public override string ImageName => "RecentlyLootedWindow.png";

    public override void OnEnable() {
        items = [];

        addonRecentlyLooted = new NodeListAddon {
            NativeController = System.NativeController,
            Size = new Vector2(250.0f, 350.0f),
            InternalName = "RecentlyLooted",
            Title = "Recently Looted Items",
            OpenCommand = "/recentloot",
            UpdateListFunction = UpdateList,
        };

        addonRecentlyLooted.Initialize(new Keybind {
            Key = VirtualKey.L,
            Modifiers = [ VirtualKey.CONTROL ],
        });

        OpenConfigAction = addonRecentlyLooted.OpenAddonConfig;

        enableTracking = Services.ClientState.IsLoggedIn;

        Services.GameInventory.InventoryChanged += OnRawItemAdded;
        Services.ClientState.Login += OnLogin;
        Services.ClientState.Logout += OnLogout;

        updateRequested = true;
    }

    public override void OnDisable() {
        addonRecentlyLooted?.Dispose();
        addonRecentlyLooted = null;

        items?.Clear();
        items = null;

        Services.GameInventory.InventoryChanged -= OnRawItemAdded;
        Services.ClientState.Login -= OnLogin;
        Services.ClientState.Logout -= OnLogout;
    }

    private void OnLogin() {
        enableTracking = true;
        items?.Clear();
    }

    private void OnLogout(int type, int code)
        => enableTracking = false;

    private void OnRawItemAdded(IReadOnlyCollection<InventoryEventArgs> events) {
        if (!enableTracking) return;
        
        foreach (var eventData in events) {
            if (!Inventory.StandardInventories.Contains(eventData.Item.ContainerType)) continue;
            
            if (!Services.ClientState.IsLoggedIn) return;
            if (eventData is not (InventoryItemAddedArgs or InventoryItemChangedArgs)) return;
            if (eventData is InventoryItemChangedArgs changedArgs && changedArgs.OldItemState.Quantity >= changedArgs.Item.Quantity) return;

            var inventoryItem = (InventoryItem*)eventData.Item.Address;
            var changeAmount = eventData is InventoryItemChangedArgs changed ? changed.Item.Quantity - changed.OldItemState.Quantity : eventData.Item.Quantity;
        
            items?.Add(new LootedItemInfo(
                items.Count, 
                inventoryItem->GetItemId(), 
                inventoryItem->GetIconId(), 
                inventoryItem->GetItemName(), 
                changeAmount)
            );

            updateRequested = true;
        }
    }
    
    private bool UpdateList(VerticalListNode listNode, bool isOpening) {
        if (!updateRequested && !isOpening) return false;
        if (items is null) return false;
        
        var listUpdated = listNode.SyncWithListData(items, node => node.Item, data => new LootItemNode {
            Size = new Vector2(listNode.Width, 36.0f),
            Item = data,
        });

        listNode.ReorderNodes(Comparison);

        updateRequested = false;
        return listUpdated;
    }
    
    private static int Comparison(NodeBase x, NodeBase y) {
        if (x is not LootItemNode left ||  y is not LootItemNode right) return 0;
        
        return left.Item.Index > right.Item.Index ? -1 : 1;
    }
}
