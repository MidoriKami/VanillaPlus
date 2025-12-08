using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using LuminaSupplemental.Excel.Model;
using LuminaSupplemental.Excel.Services;

namespace VanillaPlus.Features.DutyLootPreview;

public class DutyLootItem {
    public required uint ItemId { get; init; }
    public required uint IconId { get; init; }
    public ReadOnlySeString Name { get; private init; }
    public required uint ItemSortCategory { get; init; } 
    public required bool CanTryOn { get; init; }

    /// <summary>
    /// Get the loot items available for some content
    /// </summary>
    public static IEnumerable<DutyLootItem> ForContent(uint contentId) {
        var bossDropItemIds = LoadItems<DungeonBossDrop>(CsvLoader.DungeonBossDropResourceName)
            .Where(drop => drop.ContentFinderConditionId == contentId)
            .Select(drop => drop.ItemId);

        var bossChestDropItemIds = LoadItems<DungeonBossChest>(CsvLoader.DungeonBossChestResourceName)
            .Where(drop => drop.ContentFinderConditionId == contentId)
            .Select(drop => drop.ItemId);

        var dungeonChestIds = LoadItems<DungeonChest>(CsvLoader.DungeonChestResourceName)
            .Where(chest => chest.ContentFinderConditionId == contentId)
            .Select(chest => chest.RowId)
            .ToHashSet();

        var dungeonChestDropItemIds = LoadItems<DungeonChestItem>(CsvLoader.DungeonChestItemResourceName)
            .Where(drop => dungeonChestIds.Contains(drop.ChestId))
            .Select(drop => drop.ItemId);

        var itemIds = bossDropItemIds
            .Concat(bossChestDropItemIds)
            .Concat(dungeonChestDropItemIds)
            .Where(id => id != 0)
            .Distinct();

        var items = itemIds.Select(itemId => Services.DataManager.GetItem(itemId));
        foreach (var item in items) {
            if (item.Icon == 0 || item.Name.IsEmpty)
                continue;

            yield return new DutyLootItem {
                ItemId = item.RowId,
                IconId = item.Icon,
                Name = item.Name,
                ItemSortCategory = item.ItemSortCategory.RowId,
                CanTryOn = CheckCanTryOn(item),
            };
        }
    }

    private static IEnumerable<T> LoadItems<T>(string resourceName) where T : ICsv, new() {
        return CsvLoader.LoadResource<T>(
            resourceName: resourceName,
            includesHeaders: false,
            out _,
            out _,
            Services.DataManager.GameData,
            Services.DataManager.GameData.Options.DefaultExcelLanguage
        );
    }

    // See: https://github.com/Haselnussbomber/HaselCommon/blob/30c023516c0f9771183bbb5c01eb8122765e8bd0/HaselCommon/Services/ItemService.cs#L298-L327
    private static bool CheckCanTryOn(Item item) {
        // not equippable, Waist or SoulCrystal => false
        if (item.EquipSlotCategory.RowId is 0 or 6 or 17)
            return false;

        // any OffHand that's not a Shield => false
        if (item.EquipSlotCategory.RowId is 2 && item.FilterGroup != 3) // 3 = Shield
            return false;

        var race = (int)Services.PlayerState.Race.RowId;
        if (race == 0)
            return false;

        return true;
    }
}
