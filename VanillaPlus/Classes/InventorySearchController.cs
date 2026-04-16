using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using Lumina.Extensions;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public static unsafe class InventorySearchController {
    public static void FadeInventoryNodes(AtkUnitBase* addon, string searchString) {
        var isDisallowedInventory = IsDisallowedInventory(addon);
        var inventorySorter = Inventory.GetSorterForInventory(addon);

        foreach (var childAddon in Inventory.GetInventoryAddons(addon)) {
            var inventorySlots = Inventory.GetInventorySlots(childAddon);

            foreach (var index in Enumerable.Range(0, inventorySlots.Length)) {
                var inventorySlot = inventorySlots[index].Value;
                if (inventorySlot is null) continue;

                var adjustedPage = Inventory.GetAdjustedPage(childAddon, index);
                var adjustedIndex = Inventory.GetAdjustedIndex(childAddon, index);

                var item = Inventory.GetItemForSorter(inventorySorter, adjustedPage, adjustedIndex);
                if (item is null) continue;

                if (item->IsRegexMatch(searchString) || isDisallowedInventory) {
                    inventorySlot->OwnerNode->FadeNode(0.0f);
                }
                else {
                    inventorySlot->OwnerNode->FadeNode(0.5f);
                }
            }
        }
    }

    private static bool IsDisallowedInventory(AtkUnitBase* addon) => addon->NameString switch {
        "InventoryExpansion" when Inventory.GetTabForInventory(addon) is 1 => true,
        "InventoryLarge" when Inventory.GetTabForInventory(addon) is 2 or 3 => true,
        "Inventory" when Inventory.GetTabForInventory(addon) is 4 => true,
        _ => false,
    };
}
