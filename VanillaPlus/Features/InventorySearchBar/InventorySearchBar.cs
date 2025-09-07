using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit;
using KamiToolKit.Nodes;
using Lumina.Extensions;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using SeStringBuilder = Lumina.Text.SeStringBuilder;

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

    private ImageNode? expandedHelpNode;
    private ImageNode? largeHelpNode;
    private ImageNode? inventoryHelpNode;

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

    private static TextInputNode GetInputTextNode(Action<SeString> searchCallback, Vector2 headerSize, Vector2 searchBoxSize) => new() {
        Position = headerSize / 2.0f - searchBoxSize / 2.0f + new Vector2(0.0f, 10.0f),
        Size = searchBoxSize,
        PlaceholderString = "Search . . .",
        OnInputReceived = searchCallback,
        IsVisible = true,
    };

    private static ImageNode GetTooltipNode(Vector2 headerSize, Vector2 searchBoxSize) => new SimpleImageNode {
        Position = headerSize / 2.0f + new Vector2(searchBoxSize.X / 2.0f, - searchBoxSize.Y / 2) + new Vector2(5.0f, 10.0f),
        Size = new Vector2(28.0f, 28.0f),
        TexturePath = "ui/uld/CircleButtons.tex",
        TextureCoordinates = new Vector2(112.0f, 84.0f),
        TextureSize = new Vector2(28.0f, 28.0f),
        Tooltip = new SeStringBuilder()
            .Append("[VanillaPlus]: Supports Regex Search")
            .AppendNewLine()
            .Append("Start input with '$' to search by description")
            .ToReadOnlySeString()
            .ToDalamudString(),
        EventFlagsSet = true,
        IsVisible = true,
    };

    private void AttachExpansionNodes(AddonInventoryExpansion* addon) {
        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
        var searchBoxSize = new Vector2(250.0f, 28.0f);

        expandedInventorySearchBoxNode = GetInputTextNode(searchString => UpdateInventoryExpansion(addon, searchString), headerSize, searchBoxSize);
        System.NativeController.AttachNode(expandedInventorySearchBoxNode, addon->WindowNode);
        
        expandedHelpNode = GetTooltipNode(headerSize, searchBoxSize);
        System.NativeController.AttachNode(expandedHelpNode, addon->WindowNode);
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
        System.NativeController.DisposeNode(ref expandedHelpNode);
    }

    private void AttachLargeNodes(AddonInventoryLarge* addon) {
        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
        var searchBoxSize = new Vector2(250.0f, 28.0f);
        
        largeInventorySearchBoxNode = GetInputTextNode(searchString => UpdateInventoryLarge(addon, searchString), headerSize, searchBoxSize);
        System.NativeController.AttachNode(largeInventorySearchBoxNode, addon->WindowNode);
        
        largeHelpNode = GetTooltipNode(headerSize, searchBoxSize);
        System.NativeController.AttachNode(largeHelpNode, addon->WindowNode);
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
        System.NativeController.DisposeNode(ref largeHelpNode);
    }

    private void AttachInventoryNodes(AddonInventory* addon) {
        var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
        var searchBoxSize = new Vector2(100.0f, 28.0f);
        
        inventorySearchBoxNode = GetInputTextNode(searchString => UpdateInventory(addon, searchString), headerSize, searchBoxSize);
        System.NativeController.AttachNode(inventorySearchBoxNode, addon->WindowNode);
        
        inventoryHelpNode = GetTooltipNode(headerSize, searchBoxSize);
        System.NativeController.AttachNode(inventoryHelpNode, addon->WindowNode);
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
        System.NativeController.DisposeNode(ref inventoryHelpNode);
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
