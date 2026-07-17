using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.RecentlyLootedWindow.Addons;

public class RecentlyLootedListAddon : NativeAddon {

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        listNode = new ListNode<LootedItemInfo, LootedItemListItemNode> {
            Position = ContentStartPosition,
            Size = ContentSize,
            OptionsList = lootedItems,
        };

        listNode.AttachNode(this);
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        base.OnFinalize(addon);

        listNode = null;
    }

    // It makes sense to do the tracking here,
    // as this is where the data is consumed,
    // so it doesn't have to be passed back and forth with the module.
    public RecentlyLootedListAddon() {
        enableTracking = IClientState.Get().IsLoggedIn;

        IGameInventory.Get().InventoryChanged += OnRawItemAdded;
        IClientState.Get().Login += OnLogin;
        IClientState.Get().Logout += OnLogout;
    }

    public override async ValueTask DisposeAsync() {
        enableTracking = false;

        IGameInventory.Get().InventoryChanged -= OnRawItemAdded;
        IClientState.Get().Login -= OnLogin;
        IClientState.Get().Logout -= OnLogout;

        await base.DisposeAsync();
    }

    private void OnLogin() {
        enableTracking = true;
        lootedItems.Clear();
    }

    private void OnLogout(int type, int code)
        => enableTracking = false;

    private unsafe void OnRawItemAdded(IReadOnlyCollection<InventoryEventArgs> events) {
        if (!enableTracking) return;

        foreach (var eventData in events) {
            if (!Inventory.StandardInventories.Contains(eventData.Item.ContainerType)) continue;

            if (!IClientState.Get().IsLoggedIn) break;
            if (eventData is not (InventoryItemAddedArgs or InventoryItemChangedArgs)) break;
            if (eventData is InventoryItemChangedArgs changedArgs && changedArgs.OldItemState.Quantity >= changedArgs.Item.Quantity) break;

            var inventoryItem = (InventoryItem*)eventData.Item.Address;
            var changeAmount = eventData is InventoryItemChangedArgs changed ? changed.Item.Quantity - changed.OldItemState.Quantity : eventData.Item.Quantity;

            lootedItems = [
                new LootedItemInfo(inventoryItem->GetItemId(),
                    inventoryItem->IconId,
                    inventoryItem->Name,
                    changeAmount),
                .. lootedItems,
            ];
        }

        listNode?.OptionsList = lootedItems;
    }

    private bool enableTracking;
    private List<LootedItemInfo> lootedItems = [];
    private ListNode<LootedItemInfo, LootedItemListItemNode>? listNode;
}
