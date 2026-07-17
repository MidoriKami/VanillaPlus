using System.Text.RegularExpressions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace VanillaPlus.Classes;

public record ItemStack(InventoryItem Item, int Quantity) {

    public string ItemName {
        get {
            var inventoryItem = Item;
            return inventoryItem.Name.ToString();
        }
    }

    public static bool IsMatch(ItemStack itemStack, string searchTerms) {
        if (searchTerms == string.Empty) return true;

        var regex = new Regex(searchTerms, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        var itemInfo = Service<IDataManager>.Get().GetItem(itemStack.Item.ItemId);

        if (regex.IsMatch(itemInfo.Name.ToString())) return true;
        if (regex.IsMatch(itemInfo.LevelEquip.ToString())) return true;
        if (regex.IsMatch(itemInfo.LevelItem.RowId.ToString())) return true;

        return false;
    }
}
