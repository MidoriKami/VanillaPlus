using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using VanillaPlus.NativeElements.Addons;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.ListInventory;

public class AddonListInventory : SearchableNodeListAddon<ItemStack, InventoryItemNode> {
    private InventoryFilterMode lastSortingMode = InventoryFilterMode.Alphabetical;
    private bool isReversed;
    private string lastSearchString = string.Empty;
    
    public AddonListInventory() {
        OnSortingUpdated = UpdateSorting;
        OnSearchUpdated = UpdateSearch;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        base.OnSetup(addon);

        addon->SubscribeNumberArrayData(NumberArrayType.Inventory);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "Inventory", OnInventoryUpdate);
    }

    protected override unsafe void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData) {
        UpdateInventoryItems();
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        base.OnFinalize(addon);
        
        addon->UnsubscribeNumberArrayData(NumberArrayType.Inventory);
        Services.AddonLifecycle.UnregisterListener(OnInventoryUpdate);
    }

    private void OnInventoryUpdate(AddonEvent type, AddonArgs args) {
        UpdateInventoryItems();
    }

    private void UpdateInventoryItems() {
        Services.PluginLog.Debug("Inventory Updated");
        ListItems = Inventory.GetInventoryStacks().Where(item => IsRegexMatch(item, lastSearchString)).ToList();
        ListItems.Sort((left, right) => Comparison(left, right, lastSortingMode) * (isReversed ? -1 : 1));
    }
    
    private void UpdateSorting(string newFilterString, bool reversed) {
        var enumValue = newFilterString.ParseAsEnum(InventoryFilterMode.Alphabetical);

        lastSortingMode = enumValue;
        isReversed = reversed;
        
        UpdateInventoryItems();
    }
    
    private void UpdateSearch(string searchString) {
        lastSearchString = searchString;
        
        UpdateInventoryItems();
    }
    
    private static int Comparison(ItemStack left, ItemStack right, InventoryFilterMode filterMode) {
        var leftItem = Services.DataManager.GetExcelSheet<Item>().GetRow(left.Item.ItemId);
        var rightItem = Services.DataManager.GetExcelSheet<Item>().GetRow(right.Item.ItemId);
    
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
    
    private static bool IsRegexMatch(ItemStack itemStack, string searchTerms) {
        if (searchTerms == string.Empty) return true;
        
        var regex = new Regex(searchTerms, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        var itemInfo = Services.DataManager.GetExcelSheet<Item>().GetRow(itemStack.Item.ItemId);
        
        if (regex.IsMatch(itemInfo.Name.ToString())) return true;
        if (regex.IsMatch(itemInfo.LevelEquip.ToString())) return true;
        if (regex.IsMatch(itemInfo.LevelItem.RowId.ToString())) return true;
    
        return false;
    }
}
