using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Addons;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public unsafe class RecentlyLootedWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_RecentlyLootedWindow,
        Description = Strings.ModificationDescription_RecentlyLootedWindow,
        Type = ModificationType.NewWindow,
        Authors = ["MidoriKami"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Limit tracking to standard inventories, and armory"),
            new ChangeLogInfo(3, "Displays item quantity as text over icon instead of appended to the end of name"),
        ],
    };

    private NodeListAddon<LootedItemInfo, LootedItemListItemNode>? addonRecentlyLooted;

    private bool enableTracking;

    public override string ImageName => "RecentlyLootedWindow.png";

    public override void OnEnable() {
        addonRecentlyLooted = new NodeListAddon<LootedItemInfo, LootedItemListItemNode> {
            Size = new Vector2(250.0f, 350.0f),
            InternalName = "RecentlyLooted",
            Title = Strings.RecentlyLootedWindow_Title,
            OpenCommand = "/recentloot",
            ListItems = [],
            ItemSpacing = 2.0f,
        };

        addonRecentlyLooted.Initialize();

        OpenConfigAction = addonRecentlyLooted.OpenAddonConfig;

        enableTracking = Services.ClientState.IsLoggedIn;

        Services.GameInventory.InventoryChanged += OnRawItemAdded;
        Services.ClientState.Login += OnLogin;
        Services.ClientState.Logout += OnLogout;
    }

    public override void OnDisable() {
        addonRecentlyLooted?.Dispose();
        addonRecentlyLooted = null;

        Services.GameInventory.InventoryChanged -= OnRawItemAdded;
        Services.ClientState.Login -= OnLogin;
        Services.ClientState.Logout -= OnLogout;
    }

    private void OnLogin() {
        enableTracking = true;
        addonRecentlyLooted?.ListItems.Clear();
    }

    private void OnLogout(int type, int code)
        => enableTracking = false;

    private void OnRawItemAdded(IReadOnlyCollection<InventoryEventArgs> events) {
        if (!enableTracking) return;
        if (addonRecentlyLooted is null) return;
        
        foreach (var eventData in events) {
            if (!Inventory.StandardInventories.Contains(eventData.Item.ContainerType)) continue;
            
            if (!Services.ClientState.IsLoggedIn) break;
            if (eventData is not (InventoryItemAddedArgs or InventoryItemChangedArgs)) break;
            if (eventData is InventoryItemChangedArgs changedArgs && changedArgs.OldItemState.Quantity >= changedArgs.Item.Quantity) break;

            var inventoryItem = (InventoryItem*)eventData.Item.Address;
            var changeAmount = eventData is InventoryItemChangedArgs changed ? changed.Item.Quantity - changed.OldItemState.Quantity : eventData.Item.Quantity;

            addonRecentlyLooted.ListItems = [ 
                new LootedItemInfo(inventoryItem->GetItemId(),
                    inventoryItem->IconId,
                    inventoryItem->Name,
                    changeAmount),
                ..addonRecentlyLooted.ListItems,
            ];
        }

        addonRecentlyLooted.RefreshList();
    }
}
