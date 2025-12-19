using FFXIVClientStructs.FFXIV.Client.Game;
using VanillaPlus.Utilities;

namespace VanillaPlus.Extensions;

public static class InventoryManagerExtensions {
    extension(ref InventoryManager inventoryManager) {
        /// <summary>
        ///     Checks Inventory, Equipped, and Armory by default for item, does not search all inventories, unless specified.
        /// </summary>
        public bool PlayerHasItem(uint itemId, params InventoryType[] inventoryTypes) {
            if (inventoryTypes.Length is 0) {
                inventoryTypes = Inventory.StandardInventories.ToArray();
            }
        
            foreach (var inventory in inventoryTypes) {
                if (inventoryManager.GetItemCountInContainer(itemId, inventory) is not 0) {
                    return true;
                }
            }

            return false;
        }
    }
}
