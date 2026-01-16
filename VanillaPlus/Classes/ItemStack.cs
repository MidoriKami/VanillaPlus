using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace VanillaPlus.Classes;

public record ItemStack(InventoryItem Item, int Quantity) {

    public static int Comparison(ItemStack left, ItemStack right, InventoryFilterMode filterMode) {
        var leftItem = Services.DataManager.GetItem(left.Item.ItemId);
        var rightItem = Services.DataManager.GetItem(right.Item.ItemId);
    
        var result = filterMode switch {
            InventoryFilterMode.Alphabetical => string.CompareOrdinal(leftItem.Name.ToString(), rightItem.Name.ToString()),
            InventoryFilterMode.ClassJobLevel => rightItem.LevelEquip.CompareTo(leftItem.LevelEquip),
            InventoryFilterMode.ItemLevel  => rightItem.LevelItem.RowId.CompareTo(leftItem.LevelItem.RowId),
            InventoryFilterMode.Rarity  => rightItem.Rarity.CompareTo(leftItem.Rarity),
            InventoryFilterMode.ItemId => rightItem.RowId.CompareTo(leftItem.RowId),
            InventoryFilterMode.ItemCategory => rightItem.ItemUICategory.RowId.CompareTo(leftItem.ItemUICategory.RowId),
            InventoryFilterMode.Quantity => left.Quantity.CompareTo(right.Quantity),
            _ => string.CompareOrdinal(leftItem.Name.ToString(), rightItem.Name.ToString()),
        };
    
        return result is 0 ? string.CompareOrdinal(leftItem.Name.ToString(), rightItem.Name.ToString()) : result;
    }
    
    public static bool IsMatch(ItemStack itemStack, string searchTerms) {
        if (searchTerms == string.Empty) return true;
        
        var regex = new Regex(searchTerms, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        var itemInfo = Services.DataManager.GetItem(itemStack.Item.ItemId);
        
        if (regex.IsMatch(itemInfo.Name.ToString())) return true;
        if (regex.IsMatch(itemInfo.LevelEquip.ToString())) return true;
        if (regex.IsMatch(itemInfo.LevelItem.RowId.ToString())) return true;
    
        return false;
    }
}
