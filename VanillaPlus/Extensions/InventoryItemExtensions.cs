using System.Diagnostics.CodeAnalysis;
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

        Services.PluginLog.Debug($"Resolved {item.GetItemId()}'s iconId to: {iconId}");
        return iconId;
    }

    public static ReadOnlySeString GetItemName(ref this InventoryItem item) {
        var itemId = item.GetItemId();
        var itemName = ItemUtil.GetItemName(itemId);

        Services.PluginLog.Debug($"Resolved {itemId}'s name to to: {itemName}");
        
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
}
