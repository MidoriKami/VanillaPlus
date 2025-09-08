using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit;
using Lumina.Extensions;
using VanillaPlus.Basic_Nodes;
using VanillaPlus.Classes;

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

    private TextInputWithHintNode? expandedInputText;
    private TextInputWithHintNode? largeInputText;
    private TextInputWithHintNode? inventoryInputText;

    private int inventoryLargeSelectedTab;
    private int inventorySelectedTab;
    
    public override void OnEnable() {
        AddExpandedInventoryController();
        AddLargeInventoryController();
        AddInventoryController();
    }

    public override void OnDisable() {
        expandedInventoryController?.Dispose();
        expandedInventoryController = null;
        
        largeInventoryController?.Dispose();
        largeInventoryController = null;

        inventoryController?.Dispose();
        inventoryController = null;
    }

    private void AddExpandedInventoryController() {
        expandedInventoryController = new AddonController<AddonInventoryExpansion>("InventoryExpansion");
        expandedInventoryController.OnAttach += addon => {
            var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
            var searchBoxSize = new Vector2(275.0f, 28.0f);

            expandedInputText = new TextInputWithHintNode {
                Position = headerSize / 2.0f - searchBoxSize / 2.0f + new Vector2(25.0f, 10.0f),
                Size = searchBoxSize,
                OnInputReceived = searchString => UpdateInventoryExpansion(addon, searchString),
                IsVisible = true,
            };
            System.NativeController.AttachNode(expandedInputText, addon->WindowNode);
        };

        expandedInventoryController.OnUpdate += addon => {
            if (expandedInputText is null) return;
            if (inventoryLargeSelectedTab != addon->TabIndex) {
                UpdateInventoryExpansion(addon, expandedInputText.SearchString);
            }
        
            inventoryLargeSelectedTab = addon->TabIndex;
        };

        expandedInventoryController.OnDetach += addon => {
            UpdateInventoryExpansion(addon, string.Empty);
            System.NativeController.DisposeNode(ref expandedInputText);
        };

        expandedInventoryController.Enable();
    }

    private void AddLargeInventoryController() {
        largeInventoryController = new AddonController<AddonInventoryLarge>("InventoryLarge");
        largeInventoryController.OnAttach += addon => {
            var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
            var searchBoxSize = new Vector2(250.0f, 28.0f);
        
            largeInputText = new TextInputWithHintNode {
                Position = headerSize / 2.0f - searchBoxSize / 2.0f + new Vector2(25.0f, 10.0f),
                Size = searchBoxSize,
                OnInputReceived = searchString => UpdateInventoryLarge(addon, searchString),
                IsVisible = true,
            };
            System.NativeController.AttachNode(largeInputText, addon->WindowNode);
        };

        largeInventoryController.OnUpdate += addon => {
            if (largeInputText is null) return;
            if (inventoryLargeSelectedTab != addon->TabIndex) {
                UpdateInventoryLarge(addon, largeInputText.SearchString);
            }
        
            inventoryLargeSelectedTab = addon->TabIndex;
        };

        largeInventoryController.OnDetach += addon => {
            UpdateInventoryLarge(addon, string.Empty);
            System.NativeController.DisposeNode(ref largeInputText);
        };

        largeInventoryController.Enable();
    }

    private void AddInventoryController() {
        inventoryController = new AddonController<AddonInventory>("Inventory");
        inventoryController.OnAttach += addon => {
            var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
            var searchBoxSize = new Vector2(150.0f, 28.0f);

            inventoryInputText = new TextInputWithHintNode {
                Position = headerSize / 2.0f - searchBoxSize / 2.0f + new Vector2(25.0f, 10.0f),
                Size = searchBoxSize,
                OnInputReceived = searchString => UpdateInventory(addon, searchString),
                IsVisible = true,
            };
            System.NativeController.AttachNode(inventoryInputText, addon->WindowNode);
        };

        inventoryController.OnUpdate += addon => {
            if (inventoryInputText is null) return;
            if (inventorySelectedTab != addon->TabIndex) {
                UpdateInventory(addon, inventoryInputText.SearchString);
            }

            inventorySelectedTab = addon->TabIndex;
        };
        
        inventoryController.OnDetach += addon => {
            UpdateInventory(addon, string.Empty);
            System.NativeController.DisposeNode(ref inventoryInputText);
        };

        inventoryController.Enable();
    }

    private static void UpdateInventoryExpansion(AddonInventoryExpansion* _, SeString searchString) {
        string[] inventoryGridNames = [ "InventoryGrid0E", "InventoryGrid1E", "InventoryGrid2E", "InventoryGrid3E" ];

        foreach (var inventoryType in Enumerable.Range(0, 4)) {
            var inventoryName = inventoryGridNames[inventoryType];
            var inventoryGrid = Services.GameGui.GetAddonByName<AddonInventoryGrid>(inventoryName);
            if (inventoryGrid is null) continue;

            FadeInventoryNodes(searchString, inventoryGrid, inventoryType);
        }
    }

    private static void UpdateInventoryLarge(AddonInventoryLarge* addon, SeString searchString) {
        string[] inventoryGridNames = [ "InventoryGrid0", "InventoryGrid1" ];

        var selectedTab = addon->TabIndex;

        foreach (var inventoryType in Enumerable.Range(0, 2)) {
            var inventoryName = inventoryGridNames[inventoryType];
            var inventoryGrid = Services.GameGui.GetAddonByName<AddonInventoryGrid>(inventoryName);
            if (inventoryGrid is null) continue;

            FadeInventoryNodes(searchString, inventoryGrid, inventoryType + selectedTab * 2);
        }
    }

    private static void UpdateInventory(AddonInventory* addon, SeString searchString) {
        var selectedTab = addon->TabIndex;
        var inventoryGrid = Services.GameGui.GetAddonByName<AddonInventoryGrid>("InventoryGrid");

        FadeInventoryNodes(searchString, inventoryGrid, selectedTab);
    }

    private static void FadeInventoryNodes(SeString searchString, AddonInventoryGrid* inventoryGrid, int inventoryType) {
        var sorter = ItemOrderModule.Instance()->InventorySorter;

        foreach (var index in Enumerable.Range(0, inventoryGrid->Slots.Length)) {
            var sorterItem = sorter->Items
                .FirstOrNull(item => item.Value->Page == inventoryType && item.Value->Slot == index);
            if (sorterItem is null) continue;

            var inventoryItem = sorter->GetInventoryItem(sorterItem);
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
}
