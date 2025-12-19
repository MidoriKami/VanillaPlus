using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using Lumina.Extensions;

namespace VanillaPlus.Classes;

public static unsafe class InventorySearchController {
    public static void FadeInventoryNodes(AtkUnitBase* addon, string searchString) {
        var isDisallowedInventory = IsDisallowedInventory(addon);
        var inventorySorter = GetSorterForInventory(addon);

        foreach (var childAddon in GetInventoryAddons(addon)) {
            var inventorySlots = GetInventorySlots(childAddon);

            foreach (var index in Enumerable.Range(0, inventorySlots.Length)) {
                var inventorySlot = inventorySlots[index].Value;
                if (inventorySlot is null) continue;

                var adjustedPage = GetAdjustedPage(childAddon, index);
                var adjustedIndex = GetAdjustedIndex(childAddon, index);

                var item = GetItemForSorter(inventorySorter, adjustedPage, adjustedIndex);
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
        "InventoryExpansion" when GetTabForInventory(addon) is 1 => true,
        "InventoryLarge" when GetTabForInventory(addon) is 2 or 3 => true,
        "Inventory" when GetTabForInventory(addon) is 4 => true,
        _ => false,
    };

    private static InventoryItem* GetItemForSorter(ItemOrderModuleSorter* sorter, int page, int slot) {
        var sorterItem = sorter->Items.FirstOrNull(item => item.Value->Page == page && item.Value->Slot == slot);
        if (sorterItem is null) return null;

        return sorter->GetInventoryItem(sorterItem);
    }

    private static ItemOrderModuleSorter* GetSorterForInventory(AtkUnitBase* addon) {
        if (addon is null) return null;

        switch (addon->NameString) {
            case "InventoryExpansion":
            case "InventoryLarge":
            case "Inventory":
                return ItemOrderModule.Instance()->InventorySorter;

            case "ArmouryBoard" when GetTabForInventory(addon) is var tab:
                return ItemOrderModule.Instance()->ArmourySorter[tab].Value;

            case "InventoryRetainerLarge":
            case "InventoryRetainer":
                return ItemOrderModule.Instance()->GetActiveRetainerSorter();

            case "InventoryBuddy" when GetTabForInventory(addon) is var tab:
                return tab switch {
                    0 => ItemOrderModule.Instance()->SaddleBagSorter,
                    1 => ItemOrderModule.Instance()->PremiumSaddleBagSorter,
                    _ => null,
                };

            default:
                return null;
        }
    }

    private static int GetTabForParentInventory(AtkUnitBase* addon) {
        if (addon is null) return 0;

        if (addon->ParentId is not 0) {
            var parentAddon = RaptureAtkUnitManager.Instance()->GetAddonById(addon->ParentId);
            if (parentAddon is null) return 0;

            return GetTabForInventory(parentAddon);
        }

        return GetTabForInventory(addon);
    }
    
    public static int GetTabForInventory(AtkUnitBase* addon) {
        if (addon is null) return 0;

        return addon->NameString switch {
            "InventoryExpansion" => ((AddonInventoryExpansion*)addon)->TabIndex,
            "InventoryLarge" => ((AddonInventoryLarge*)addon)->TabIndex,
            "Inventory" => ((AddonInventory*)addon)->TabIndex,
            "ArmouryBoard" => ((AddonArmouryBoard*)addon)->TabIndex,
            "InventoryRetainerLarge" => ((AddonInventoryRetainerLarge*)addon)->TabIndex,
            "InventoryRetainer" => ((AddonInventoryRetainer*)addon)->TabIndex,
            "InventoryBuddy" => ((AddonInventoryBuddy*)addon)->TabIndex,
            _ => 0,
        };
    }

    private static Span<Pointer<AtkComponentDragDrop>> GetInventorySlots(AtkUnitBase* addon) {
        if (addon is null) return [];

        return addon->NameString switch {
            "ArmouryBoard" => new Span<Pointer<AtkComponentDragDrop>>((void*)((nint)addon + 0x358), 50),
            "InventoryCrystalGrid" => [],
            "InventoryGridCrystal" => [],
            "RetainerCrystalGrid" => [],
            "RetainerGridCrystal" => [],
            "InventoryBuddy" => ((AddonInventoryBuddy*)addon)->Slots,
            _ => ((AddonInventoryGrid*)addon)->Slots,
        };
    }

    private static int GetAdjustedPage(AtkUnitBase* addon, int slot) {
        if (addon is null) return 0;

        return addon->NameString switch {
            "InventoryGrid0E" => 0,
            "InventoryGrid1E" => 1,
            "InventoryGrid2E" => 2,
            "InventoryGrid3E" => 3,
            "InventoryGrid0" when GetTabForParentInventory(addon) == 0 => 0,
            "InventoryGrid1" when GetTabForParentInventory(addon) == 0 => 1,
            "InventoryGrid0" when GetTabForParentInventory(addon) == 1 => 2,
            "InventoryGrid1" when GetTabForParentInventory(addon) == 1 => 3,
            "InventoryGrid" => GetTabForParentInventory(addon),
            "RetainerGrid0" => (slot + 0 * 35) / 25,
            "RetainerGrid1" => (slot + 1 * 35) / 25,
            "RetainerGrid2" => (slot + 2 * 35) / 25,
            "RetainerGrid3" => (slot + 3 * 35) / 25,
            "RetainerGrid4" => (slot + 4 * 35) / 25,
            "RetainerGrid" => (slot + GetTabForParentInventory(addon) * 35) / 25,
            "InventoryBuddy" => slot / 35,
            _ => 0,
        };
    }

    private static int GetAdjustedIndex(AtkUnitBase* addon, int slot) {
        if (addon is null) return 0;

        return addon->NameString switch {
            "RetainerGrid0" => (slot + 0 * 35) % 25,
            "RetainerGrid1" => (slot + 1 * 35) % 25,
            "RetainerGrid2" => (slot + 2 * 35) % 25,
            "RetainerGrid3" => (slot + 3 * 35) % 25,
            "RetainerGrid4" => (slot + 4 * 35) % 25,
            "RetainerGrid" => (slot + GetTabForInventory(addon) * 35) % 25,
            "InventoryBuddy" => slot % 35,
            _ => slot,
        };
    }

    private static List<Pointer<AtkUnitBase>> GetInventoryAddons(AtkUnitBase* addon) {
        if (addon is null) return [];

        return addon->NameString switch {
            "InventoryExpansion" => GetChildAddons(ref ((AddonInventoryExpansion*)addon)->AddonControl),
            "InventoryLarge" => GetChildAddons(ref ((AddonInventoryLarge*)addon)->AddonControl),
            "Inventory" => GetChildAddons(ref ((AddonInventory*)addon)->AddonControl),
            "ArmouryBoard" => [ addon ],
            "InventoryRetainerLarge" => GetChildAddons(ref ((AddonInventoryRetainerLarge*)addon)->AddonControl),
            "InventoryRetainer" => GetChildAddons(ref ((AddonInventoryRetainer*)addon)->AddonControl),
            "InventoryBuddy" => [ addon ],
            _ => [],
        };

        static List<Pointer<AtkUnitBase>> GetChildAddons(ref AtkAddonControl addonControl) {
            List<Pointer<AtkUnitBase>> addons = [];
            foreach (var child in addonControl.ChildAddons) {
                if (child.Value is null) continue;
                if (child.Value->AtkUnitBase is null) continue;
            
                addons.Add(child.Value->AtkUnitBase);
            }

            return addons;
        }
    }
}
