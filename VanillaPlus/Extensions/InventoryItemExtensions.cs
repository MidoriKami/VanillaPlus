using System.Text.RegularExpressions;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Extensions;

public static class InventoryItemExtensions {
    extension(ref InventoryItem item) {
        public uint IconId => item.GetIconId();
        public ReadOnlySeString Name => item.GetItemName();
        
        private uint GetIconId() {
            uint iconId = 0;
        
            if (item.GetEventItem() is { } eventItem) {
                iconId = eventItem.Icon;
            }
            else if (item.GetItem() is { } regularItem) {
                iconId = regularItem.Icon;

                if (item.IsHighQuality()) {
                    iconId += 1_000_000;
                }
            }

            return iconId;
        }

        private ReadOnlySeString GetItemName() {
            var itemId = item.GetItemId();
            var itemName = ItemUtil.GetItemName(itemId);
        
            return new Lumina.Text.SeStringBuilder()
                .PushColorType(ItemUtil.GetItemRarityColorType(itemId))
                .Append(itemName)
                .PopColorType()
                .ToReadOnlySeString();
        }

        private Item? GetItem() {
            var baseItemId = item.GetBaseItemId();

            if (ItemUtil.IsNormalItem(baseItemId) &&
                Services.DataManager.GetExcelSheet<Item>().TryGetRow(baseItemId, out var baseItem)) {
                return baseItem;
            }

            return null;
        }

        private EventItem? GetEventItem() {
            var baseItemId = item.GetBaseItemId();

            if (ItemUtil.IsEventItem(baseItemId) &&
                Services.DataManager.GetExcelSheet<EventItem>().TryGetRow(baseItemId, out var eventItem)) {
                return eventItem;
            }

            return null;
        }

        public bool IsRegexMatch(string searchString) {
            // Skip any data access if string is empty
            if (searchString.IsNullOrEmpty()) return true;

            var isDescriptionSearch = searchString.StartsWith('$');

            if (isDescriptionSearch) {
                searchString = searchString[1..];
            }

            try {
                var regex = new Regex(searchString,RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

                if (ItemUtil.IsEventItem(item.GetBaseItemId())) {
                    if (!Services.DataManager.GetExcelSheet<EventItem>().TryGetRow(item.GetBaseItemId(), out var itemData)) return false;

                    if (regex.IsMatch(item.ItemId.ToString())) return true;
                    if (regex.IsMatch(itemData.Name.ToString())) return true;
                }

                else if (ItemUtil.IsNormalItem(item.GetBaseItemId())) {
                    if (!Services.DataManager.GetExcelSheet<Item>().TryGetRow(item.GetBaseItemId(), out var itemData)) return false;

                    if (regex.IsMatch(item.ItemId.ToString())) return true;
                    if (regex.IsMatch(itemData.Name.ToString())) return true;
                    if (regex.IsMatch(itemData.Description.ToString()) && isDescriptionSearch) return true;
                    if (regex.IsMatch(itemData.LevelEquip.ToString())) return true;
                    if (regex.IsMatch(itemData.LevelItem.RowId.ToString())) return true;
                    if (regex.IsMatch(itemData.ClassJobCategory.Value.Name.ToString())) return true;
                    if (regex.IsMatch(itemData.ItemUICategory.Value.Name.ToString())) return true;
                }
            }
            catch (RegexParseException) { }

            return false;
        }
    }
}
