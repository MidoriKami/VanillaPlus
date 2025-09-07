using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit;
using KamiToolKit.Nodes;
using Lumina.Extensions;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.InventorySearchBar;

public unsafe class InventorySearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Inventory Search Bar",
        Description = "Adds a search bar to the inventory window.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private AddonController<AddonInventoryExpansion>? expandedInventoryController;
    private AddonController<AddonInventoryLarge>? largeInventoryController;
    private AddonController<AddonInventory>? inventoryController;

    private TextInputNode? expandedInventorySearchBoxNode;
    private TextInputNode? largeInventorySearchBoxNode;
    private TextInputNode? inventorySearchBoxNode;

    private int inventoryLargeSelectedTab;
    private int inventorySelectedTab;
    
    public override void OnEnable() {
        expandedInventoryController = new AddonController<AddonInventoryExpansion>("InventoryExpansion");
        expandedInventoryController.OnAttach += AttachExpansionNodes;
        expandedInventoryController.OnDetach += DetachExpansionNodes;
        expandedInventoryController.Enable();
        
        largeInventoryController = new AddonController<AddonInventoryLarge>("InventoryLarge");
        largeInventoryController.OnAttach += AttachLargeNodes;
        largeInventoryController.OnUpdate += OnInventoryLargeUpdate;
        largeInventoryController.OnDetach += DetachLargeNodes;
        largeInventoryController.Enable();
        
        inventoryController = new AddonController<AddonInventory>("Inventory");
        inventoryController.OnAttach += AttachInventoryNodes;
        inventoryController.OnUpdate += OnInventoryUpdate;
        inventoryController.OnDetach += DetachInventoryNodes;
        inventoryController.Enable();
    }

    public override void OnDisable() {
        expandedInventoryController?.Dispose();
        expandedInventoryController = null;
        
        largeInventoryController?.Dispose();
        largeInventoryController = null;

        inventoryController?.Dispose();
        inventoryController = null;
    }

    private void AttachExpansionNodes(AddonInventoryExpansion* addon) {
        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
        var searchBoxSize = new Vector2(250.0f, 28.0f);
        
        expandedInventorySearchBoxNode = new TextInputNode {
            Position = headerSize / 2.0f - searchBoxSize / 2.0f + new Vector2(0.0f, 10.0f),
            Size = searchBoxSize,
            PlaceholderString = "Search . . .",
            OnInputReceived = searchString => UpdateInventoryExpansion(addon, searchString),
            IsVisible = true,
        };
        System.NativeController.AttachNode(expandedInventorySearchBoxNode, addon->WindowNode);
    }

    private static void UpdateInventoryExpansion(AddonInventoryExpansion* _, SeString searchString) {
        string[] inventoryGridNames = [
            "InventoryGrid0E",
            "InventoryGrid1E",
            "InventoryGrid2E",
            "InventoryGrid3E",
        ];

        foreach (var inventoryType in Enumerable.Range(0, 4)) {
            var inventoryName = inventoryGridNames[inventoryType];
            var inventoryGrid = Services.GameGui.GetAddonByName<AddonInventoryGrid>(inventoryName);
            if (inventoryGrid is null) continue;

            FadeInventoryNodes(searchString, inventoryGrid, inventoryType);
        }
    }

    private void DetachExpansionNodes(AddonInventoryExpansion* addon) {
        UpdateInventoryExpansion(addon, string.Empty);
        System.NativeController.DisposeNode(ref expandedInventorySearchBoxNode);
    }

    private void AttachLargeNodes(AddonInventoryLarge* addon) {
        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
        var searchBoxSize = new Vector2(250.0f, 28.0f);
        
        largeInventorySearchBoxNode = new TextInputNode {
            Position = headerSize / 2.0f - searchBoxSize / 2.0f + new Vector2(0.0f, 10.0f),
            Size = searchBoxSize,
            PlaceholderString = "Search . . .",
            OnInputReceived = searchString => UpdateInventoryLarge(addon, searchString),
            IsVisible = true,
        };
        System.NativeController.AttachNode(largeInventorySearchBoxNode, addon->WindowNode);
    }

    private static void UpdateInventoryLarge(AddonInventoryLarge* addon, SeString searchString) {
        string[] inventoryGridNames = [
            "InventoryGrid0",
            "InventoryGrid1",
        ];

        var selectedTab = addon->TabIndex;
        
        foreach (var inventoryType in Enumerable.Range(0, 2)) {
            var inventoryName = inventoryGridNames[inventoryType];
            var inventoryGrid = Services.GameGui.GetAddonByName<AddonInventoryGrid>(inventoryName);
            if (inventoryGrid is null) continue;

            FadeInventoryNodes(searchString, inventoryGrid, inventoryType + selectedTab * 2);
        }
    }
    
    private void OnInventoryLargeUpdate(AddonInventoryLarge* addon) {
        if (largeInventorySearchBoxNode is null) return;
        if (inventoryLargeSelectedTab != addon->TabIndex) {
            UpdateInventoryLarge(addon, largeInventorySearchBoxNode.SeString);
        }
        
        inventoryLargeSelectedTab = addon->TabIndex;
    }

    private void DetachLargeNodes(AddonInventoryLarge* addon) {
        UpdateInventoryLarge(addon, string.Empty);
        System.NativeController.DisposeNode(ref largeInventorySearchBoxNode);
    }

    private void AttachInventoryNodes(AddonInventory* addon) {
        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
        var searchBoxSize = new Vector2(100.0f, 28.0f);
        
        inventorySearchBoxNode = new TextInputNode {
            Position = headerSize / 2.0f - searchBoxSize / 2.0f + new Vector2(0.0f, 10.0f),
            Size = searchBoxSize,
            PlaceholderString = "Search . . .",
            OnInputReceived = searchString => UpdateInventory(addon, searchString),
            IsVisible = true,
        };
        System.NativeController.AttachNode(inventorySearchBoxNode, addon->WindowNode);
    }

    private static void UpdateInventory(AddonInventory* addon, SeString searchString) {
        var selectedTab = addon->TabIndex;
        var inventoryGrid = Services.GameGui.GetAddonByName<AddonInventoryGrid>("InventoryGrid");
        
        FadeInventoryNodes(searchString, inventoryGrid, selectedTab);
    }

    private void OnInventoryUpdate(AddonInventory* addon) {
        if (inventorySearchBoxNode is null) return;
        if (inventorySelectedTab != addon->TabIndex) {
            UpdateInventory(addon, inventorySearchBoxNode.SeString);
        }
        
        inventorySelectedTab = addon->TabIndex;
    }

    private void DetachInventoryNodes(AddonInventory* addon) {
        UpdateInventory(addon, string.Empty);
        System.NativeController.DisposeNode(ref inventorySearchBoxNode);
    }

    private static void FadeInventoryNodes(SeString searchString, AddonInventoryGrid* inventoryGrid, int inventoryType) {
        foreach (var index in Enumerable.Range(0, inventoryGrid->Slots.Length)) {
            var sorterItem = ItemOrderModule.Instance()->InventorySorter->Items
                .FirstOrNull(item => item.Value->Page == inventoryType && item.Value->Slot == index);
            if (sorterItem is null) continue;

            var inventoryItem = GetInventoryItem(ItemOrderModule.Instance()->InventorySorter, sorterItem);
            if (inventoryItem is null) continue;

            var inventorySlot = inventoryGrid->Slots[index].Value;
            if (inventorySlot is null) continue;

            var slotNode = inventorySlot->OwnerNode;
            if (slotNode is null) continue;
                
            if (inventoryItem->IsRegexMatch(searchString.ToString())) {
                slotNode->FadeNode(0.0f);
            }
            else {
                slotNode->FadeNode(0.5f);
            }
        }
    }
    
    private static long GetSlotIndex(ItemOrderModuleSorter* sorter, ItemOrderModuleSorterItemEntry* entry)
        => entry->Slot + sorter->ItemsPerPage * entry->Page;
    
    private static InventoryItem* GetInventoryItem(ItemOrderModuleSorter* sorter, ItemOrderModuleSorterItemEntry* entry)
        => GetInventoryItem(sorter, GetSlotIndex(sorter, entry));

    private static InventoryItem* GetInventoryItem(ItemOrderModuleSorter* sorter, long slotIndex) {
        if (sorter == null) return null;
        if (sorter->Items.LongCount <= slotIndex) return null;

        var item = sorter->Items[slotIndex].Value;
        if (item == null) return null;

        var container = InventoryManager.Instance()->GetInventoryContainer(sorter->InventoryType + item->Page);
        if (container == null) return null;

        return container->GetInventorySlot(item->Slot);
    }
}
