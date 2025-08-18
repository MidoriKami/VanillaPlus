using FFXIVClientStructs.FFXIV.Client.Game;
using VanillaPlus.Utilities;

namespace VanillaPlus.Extensions;

public static class InventoryManagerExtensions {
    
    /// <summary>
    /// Checks Inventory, Equipped, and Armory for item, does not search all inventories.
    /// </summary>
    public static bool PlayerHasItem(this ref InventoryManager inventoryManager, uint itemId) {
        foreach (var inventory in Inventory.StandardInventories) {
            if (inventoryManager.GetItemCountInContainer(itemId, inventory) is not 0) {
                return true;
            }
        }

        return false;
    }
}
