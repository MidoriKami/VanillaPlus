using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Extensions;

public static class InventoryItemExtensions {
    public static uint GetIconId(ref this InventoryItem item) {
        uint iconId = 0;
        
        if (item.TryGetEventItem(out var eventItem)) {
            iconId = eventItem.Value.Icon;
        }
        else if (item.TryGetItem(out var baseItem)) {
            iconId = baseItem.Value.Icon;

            if (item.IsHighQuality()) {
                iconId += 1_000_000;
            }
        }

        return iconId;
    }

    public static ReadOnlySeString GetItemName(ref this InventoryItem item) {
        var itemId = item.GetItemId();
        var itemName = ItemUtil.GetItemName(itemId);
        
        return new Lumina.Text.SeStringBuilder()
            .PushColorType(ItemUtil.GetItemRarityColorType(itemId))
            .Append(itemName)
            .PopColorType()
            .ToReadOnlySeString();
    }

    public static bool TryGetItem(ref this InventoryItem inventoryItem, [NotNullWhen(returnValue: true)] out Item? item) {
        var baseItemId = inventoryItem.GetBaseItemId();

        if (ItemUtil.IsNormalItem(baseItemId) &&
            Services.DataManager.GetExcelSheet<Item>().TryGetRow(baseItemId, out var baseItem)) {
            item = baseItem;
            return true;
        }

        item = null;
        return false;
    }

    public static bool TryGetEventItem(ref this InventoryItem inventoryItem, [NotNullWhen(returnValue: true)] out EventItem? item) {
        var baseItemId = inventoryItem.GetBaseItemId();

        if (ItemUtil.IsEventItem(baseItemId) &&
            Services.DataManager.GetExcelSheet<EventItem>().TryGetRow(baseItemId, out var eventItem)) {
            item = eventItem;
            return true;
        }

        item = null;
        return false;
    }

    public static bool IsRegexMatch(this ref InventoryItem item, string searchString) {
        const RegexOptions regexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;

        var itemData = Services.DataManager.GetExcelSheet<Item>().GetRow(item.ItemId);

        if (Regex.IsMatch(item.ItemId.ToString(), searchString)) return true;
        if (Regex.IsMatch(itemData.Name.ToString(), searchString, regexOptions)) return true;
        if (Regex.IsMatch(itemData.Description.ToString(), searchString, regexOptions) && searchString.StartsWith('$')) return true;
        if (Regex.IsMatch(itemData.LevelEquip.ToString(), searchString, regexOptions)) return true;
        if (Regex.IsMatch(itemData.LevelItem.RowId.ToString(), searchString, regexOptions)) return true;

        return false;
    }
}
