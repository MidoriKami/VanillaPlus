﻿using System.Collections.Generic;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;

namespace VanillaPlus.RecentlyLootedWindow;

public class AddonRecentlyLooted(RecentlyLootedWindowConfig config) : NativeAddon {

    private readonly List<InventoryEventArgs> itemEvents = [];

    private ScrollingAreaNode<VerticalListNode> scrollingAreaNode = null!;

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        scrollingAreaNode = new ScrollingAreaNode<VerticalListNode> {
            Position = ContentStartPosition,
            Size = ContentSize,
            IsVisible = true,
            ContentHeight = 100,
        };
        scrollingAreaNode.ContentNode.FitContents = true;
        AttachNode(scrollingAreaNode);

        RebuildList();
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        config.WindowPosition = Position;
        config.Save();
    }

    public void AddInventoryItem(InventoryEventArgs itemEvent) {
        itemEvents.Add(itemEvent);

        if (IsOpen) {
            RebuildList();
        }
    }

    private void RebuildList() {
        scrollingAreaNode.ContentNode.Clear();

        for (var index = itemEvents.Count - 1; index >= 0; index--) {
            AddItemNode(itemEvents[index]);
        }
    }

    private void AddItemNode(InventoryEventArgs itemEvent) {
        if (itemEvent is not (InventoryItemAddedArgs or InventoryItemChangedArgs)) return;
        
        var newItemNode = new LootItemNode {
            Height = 36.0f,
            Width = scrollingAreaNode.ContentNode.Width,
            IsVisible = true,
        };

        switch (itemEvent) {
            case InventoryItemAddedArgs added:
                newItemNode.SetItem(added);
                break;
            
            case InventoryItemChangedArgs changed when changed.OldItemState.Quantity < changed.Item.Quantity:
                newItemNode.SetItem(changed);
                break;
            
            default:
                return;
        }

        scrollingAreaNode.ContentNode.AddNode(newItemNode);
        scrollingAreaNode.ContentHeight = scrollingAreaNode.ContentNode.Height;
    }
}
