using System.ComponentModel;

namespace VanillaPlus.Enums;

public enum InventoryFilterMode {
    [Description(nameof(Strings.ListInventory_FilterAlphabetically))]
    Alphabetical,
    
    [Description(nameof(Strings.ListInventory_FilterQuantity))]
    Quantity,
    
    [Description(nameof(Strings.ListInventory_FilterLevel))]
    ClassJobLevel,
    
    [Description(nameof(Strings.ListInventory_FilterItemLevel))]
    ItemLevel,
    
    [Description(nameof(Strings.ListInventory_FilterRarity))]
    Rarity,
    
    [Description(nameof(Strings.ListInventory_FilterItemId))]
    ItemId,
    
    [Description(nameof(Strings.ListInventory_FilterItemCategory))]
    ItemCategory,
}
