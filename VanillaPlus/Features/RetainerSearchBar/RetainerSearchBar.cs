using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using KamiToolKit;
using Lumina.Extensions;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Nodes;

namespace VanillaPlus.Features.RetainerSearchBar;

public unsafe class RetainerSearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Retainer Search Bar",
        Description = "Adds a search bar to the retainer window.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private AddonController<AddonInventoryRetainerLarge>? largeInventoryController;
    private AddonController<AddonInventoryRetainer>? inventoryController;

    private TextInputWithHintNode? largeInputText;
    private TextInputWithHintNode? inventoryInputText;

    private int inventoryLargeSelectedTab;
    private int inventorySelectedTab;

    public override string ImageName => "RetainerSearchBar.png";

    public override void OnEnable() {
        AddLargeInventoryController();
        AddInventoryController();
    }

    public override void OnDisable() {
        largeInventoryController?.Dispose();
        largeInventoryController = null;

        inventoryController?.Dispose();
        inventoryController = null;
    }

    private void AddLargeInventoryController() {
        largeInventoryController = new AddonController<AddonInventoryRetainerLarge>("InventoryRetainerLarge");
        largeInventoryController.OnAttach += addon => {
            var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
            var searchBoxSize = new Vector2(250.0f, 28.0f);
        
            largeInputText = new TextInputWithHintNode {
                Position = headerSize / 2.0f - searchBoxSize / 2.0f + new Vector2(25.0f, 10.0f),
                Size = searchBoxSize,
                OnInputReceived = UpdateInventoryLarge,
                IsVisible = true,
            };
            System.NativeController.AttachNode(largeInputText, addon->WindowNode);
        };

        largeInventoryController.OnUpdate += addon => {
            if (largeInputText is null) return;
            if (inventoryLargeSelectedTab != addon->TabIndex) {
                UpdateInventoryLarge(largeInputText.SearchString);
            }
        
            inventoryLargeSelectedTab = addon->TabIndex;
        };

        largeInventoryController.OnDetach += _ => {
            UpdateInventoryLarge(string.Empty);
            System.NativeController.DisposeNode(ref largeInputText);
        };

        largeInventoryController.Enable();
    }

    private void AddInventoryController() {
        inventoryController = new AddonController<AddonInventoryRetainer>("InventoryRetainer");
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

    private static void UpdateInventoryLarge(SeString searchString) {
        string[] inventoryGridNames = [ "RetainerGrid0", "RetainerGrid1", "RetainerGrid2", "RetainerGrid3", "RetainerGrid4" ];

        foreach (var inventoryType in Enumerable.Range(0, 5)) {
            var inventoryName = inventoryGridNames[inventoryType];
            var inventoryGrid = Services.GameGui.GetAddonByName<AtkUnitBase>(inventoryName);
            if (inventoryGrid is null) continue;

            FadeInventoryNodes(searchString, inventoryGrid, inventoryType);
        }
    }

    private static void UpdateInventory(AddonInventoryRetainer* addon, SeString searchString) {
        var selectedTab = addon->TabIndex;
        var inventoryGrid = Services.GameGui.GetAddonByName<AtkUnitBase>("RetainerGrid");

        FadeInventoryNodes(searchString, inventoryGrid, selectedTab);
    }

    private static void FadeInventoryNodes(SeString searchString, AtkUnitBase* inventoryGrid, int currentTab) {
        var sorter = ItemOrderModule.Instance()->GetCurrentRetainerSorter();
        var slots = new Span<Pointer<AtkComponentDragDrop>>((void*)((nint)inventoryGrid + 0x238), 35);

        foreach (var index in Enumerable.Range(0, slots.Length)) {

            var offsetIndex = index + currentTab * 35;
            var adjustedPage = offsetIndex / 25;
            var adjustedIndex = offsetIndex % 25;
            
            var sorterItem = sorter->Items
                .FirstOrNull(item => item.Value->Page == adjustedPage && item.Value->Slot == adjustedIndex);
            if (sorterItem is null) continue;

            var inventoryItem = sorter->GetInventoryItem(sorterItem);
            if (inventoryItem is null) continue;

            var inventorySlot = slots[index].Value;
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
