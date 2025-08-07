using System.Collections.Generic;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.RecentlyLootedWindow;

public class AddonRecentlyLooted(AddonConfig config) : NativeAddon {

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
        if (!Services.ClientState.IsLoggedIn) return;
        if (itemEvent is not (InventoryItemAddedArgs or InventoryItemChangedArgs)) return;
        if (itemEvent is InventoryItemChangedArgs changedArgs && changedArgs.OldItemState.Quantity >= changedArgs.Item.Quantity) return;

        itemEvents.Add(itemEvent);

        if (itemEvents.Count >= 30) {
            itemEvents.RemoveAt(0);
        }

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
        var newItemNode = new LootItemNode {
            Height = 36.0f,
            Width = scrollingAreaNode.ContentNode.Width,
            IsVisible = true,
        };

        switch (itemEvent) {
            case InventoryItemAddedArgs added:
                newItemNode.SetItem(added);
                break;

            case InventoryItemChangedArgs changed:
                newItemNode.SetItem(changed);
                break;

            default:
                return;
        }

        scrollingAreaNode.ContentNode.AddNode(newItemNode);
        scrollingAreaNode.ContentHeight = scrollingAreaNode.ContentNode.Height;
    }
}
