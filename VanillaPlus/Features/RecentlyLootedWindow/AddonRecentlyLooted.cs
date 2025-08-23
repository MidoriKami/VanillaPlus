using System.Collections.Generic;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public record IndexedItemEvent(InventoryEventArgs Event, int Index);

public class AddonRecentlyLooted(AddonConfig config) : NativeAddon {

    private readonly List<IndexedItemEvent> itemEvents = [];

    private ScrollingAreaNode<VerticalListNode>? scrollingAreaNode;
    private VerticalListNode? ListNode => scrollingAreaNode?.ContentNode;

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        scrollingAreaNode = new ScrollingAreaNode<VerticalListNode> {
            Position = ContentStartPosition,
            Size = ContentSize,
            IsVisible = true,
            ContentHeight = 100,
        };
        ListNode!.FitContents = true;
        AttachNode(scrollingAreaNode);

        RebuildList();
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        config.WindowPosition = Position;
        config.Save();
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon)
        => System.NativeController.DisposeNode(ref scrollingAreaNode);

    public void AddInventoryItem(InventoryEventArgs itemEvent) {
        if (!Services.ClientState.IsLoggedIn) return;
        if (itemEvent is not (InventoryItemAddedArgs or InventoryItemChangedArgs)) return;
        if (itemEvent is InventoryItemChangedArgs changedArgs && changedArgs.OldItemState.Quantity >= changedArgs.Item.Quantity) return;

        itemEvents.Add(new IndexedItemEvent(itemEvent, itemEvents.Count));

        if (IsOpen) {
            RebuildList();
        }
    }

    private void RebuildList() {
        if (scrollingAreaNode is null) return;
        if (ListNode is null) return;

        ListNode.SyncWithListData(itemEvents, node => node.Item, data => new LootItemNode {
            Height = 36.0f,
            Width = scrollingAreaNode.ContentNode.Width,
            IsVisible = true,
            Item = data,
        });
        
        ListNode.ReorderNodes(Comparison);
        
        scrollingAreaNode.ContentHeight = ListNode?.Height ?? 100.0f;
    }

    private static int Comparison(NodeBase x, NodeBase y) {
        if (x is not LootItemNode left ||  y is not LootItemNode right) return 0;
        
        return left.Item.Index > right.Item.Index ? -1 : 1;
    }
}
