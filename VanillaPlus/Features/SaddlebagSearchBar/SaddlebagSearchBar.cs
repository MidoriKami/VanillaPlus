using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit;
using Lumina.Extensions;
using VanillaPlus.Basic_Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.SaddlebagSearchBar;

public unsafe class SaddlebagSearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Saddlebag Search Bar",
        Description = "Adds a search bar to the saddlebag window.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private AddonController<AddonInventoryBuddy>? saddlebagInventoryController;

    private TextInputWithHintNode? inputText;
    private int inventorySelectedTab;
    
    public override void OnEnable() {
        AddExpandedInventoryController();
    }

    public override void OnDisable() {
        saddlebagInventoryController?.Dispose();
        saddlebagInventoryController = null;
    }

    private void AddExpandedInventoryController() {
        saddlebagInventoryController = new AddonController<AddonInventoryBuddy>("InventoryBuddy");
        saddlebagInventoryController.OnAttach += addon => {
            var headerSize = new Vector2(addon->WindowHeaderCollisionNode->Width, addon->WindowHeaderCollisionNode->Height);
            var searchBoxSize = new Vector2(275.0f, 28.0f);

            inputText = new TextInputWithHintNode {
                Position = headerSize / 2.0f - searchBoxSize / 2.0f + new Vector2(25.0f, 10.0f),
                Size = searchBoxSize,
                OnInputReceived = searchString => UpdateSaddlebag(addon, searchString),
                IsVisible = true,
            };
            System.NativeController.AttachNode(inputText, addon->WindowNode);
        };

        saddlebagInventoryController.OnUpdate += addon => {
            if (inputText is null) return;
            if (inventorySelectedTab != addon->TabIndex) {
                UpdateSaddlebag(addon, inputText.SearchString);
            }

            inventorySelectedTab = addon->TabIndex;
        };

        saddlebagInventoryController.OnDetach += addon => {
            UpdateSaddlebag(addon, string.Empty);
            System.NativeController.DisposeNode(ref inputText);
        };

        saddlebagInventoryController.Enable();
    }

    private static void UpdateSaddlebag(AddonInventoryBuddy* addon, SeString searchString) {
        FadeInventoryNodes(searchString, addon);
    }

    private static void FadeInventoryNodes(SeString searchString, AddonInventoryBuddy* inventoryGrid) {
        var sorter = inventoryGrid->TabIndex switch {
            0 => ItemOrderModule.Instance()->SaddleBagSorter,
            1 => ItemOrderModule.Instance()->PremiumSaddleBagSorter,
            _ => throw new Exception($"Saddlebag Tab Not Supported: {inventoryGrid->TabIndex}"),
        };

        foreach (var index in Enumerable.Range(0, inventoryGrid->Slots.Length)) {
            var sorterItem = sorter->Items
                .FirstOrNull(item => item.Value->Page == index / sorter->ItemsPerPage && item.Value->Slot == index % sorter->ItemsPerPage);
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
