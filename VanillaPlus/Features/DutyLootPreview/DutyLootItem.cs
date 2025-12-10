using System.Collections.Generic;
using System.Globalization;
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
    public required List<ReadOnlySeString> Sources { get; init; }

    private static readonly ReadOnlySeString DungeonChestSource = "Dungeon Chest";

    /// <summary>
    /// Get the loot items available for some content
    /// </summary>
    public static IEnumerable<DutyLootItem> ForContent(uint contentId) {
        // Load boss data and build lookup by FightNo
        var bosses = LoadItems<DungeonBoss>(CsvLoader.DungeonBossResourceName)
            .Where(boss => boss.ContentFinderConditionId == contentId)
            .DistinctBy(boss => boss.FightNo) // TODO: Why do some duties have duplicate FightNo's?
            .ToDictionary(boss => boss.FightNo);

        // Track sources per item
        var itemSources = new Dictionary<uint, List<ReadOnlySeString>>();

        // Process boss drops
        var bossDrops = LoadItems<DungeonBossDrop>(CsvLoader.DungeonBossDropResourceName)
            .Where(drop => drop.ContentFinderConditionId == contentId);

        foreach (var drop in bossDrops) {
            AddBossSource(drop.ItemId, drop.FightNo, bosses, itemSources);
        }

        var bossChestDrops = LoadItems<DungeonBossChest>(CsvLoader.DungeonBossChestResourceName)
            .Where(drop => drop.ContentFinderConditionId == contentId);

        foreach (var drop in bossChestDrops) {
            AddBossSource(drop.ItemId, drop.FightNo, bosses, itemSources);
        }

        var dungeonChestIds = LoadItems<DungeonChest>(CsvLoader.DungeonChestResourceName)
            .Where(chest => chest.ContentFinderConditionId == contentId)
            .Select(chest => chest.RowId)
            .ToHashSet();

        var dungeonChestItems = LoadItems<DungeonChestItem>(CsvLoader.DungeonChestItemResourceName)
            .Where(drop => dungeonChestIds.Contains(drop.ChestId));

        foreach (var drop in dungeonChestItems) {
            AddDungeonChestSource(drop.ItemId, itemSources);
        }

        foreach (var (itemId, sources) in itemSources) {
            var item = Services.DataManager.GetItem(itemId);
            if (item.Icon == 0 || item.Name.IsEmpty)
                continue;

            yield return new DutyLootItem {
                ItemId = item.RowId,
                IconId = item.Icon,
                Name = item.Name,
                ItemSortCategory = item.ItemSortCategory.RowId,
                CanTryOn = CheckCanTryOn(item),
                Sources = sources,
            };
        }
    }

    public uint SortOrder => ItemSortCategory switch {
        9 or 63 => uint.MaxValue,     // Materia - very bottom
        5 or 56 => uint.MaxValue - 1, // Equipment - bottom
        _ => ItemSortCategory,
    };

    private static void AddDungeonChestSource(uint itemId, Dictionary<uint, List<ReadOnlySeString>>? itemSources) {
        if (itemSources is null) return;
        if (itemId == 0) return;

        if (!itemSources.TryGetValue(itemId, out var sources)) {
            sources = [];
            itemSources[itemId] = sources;
        }
        if (!sources.Contains(DungeonChestSource)) {
            sources.Add(DungeonChestSource);
        }
    }

    private static void AddBossSource(uint itemId, uint fightNo, Dictionary<uint, DungeonBoss>? bosses, Dictionary<uint, List<ReadOnlySeString>>? itemSources) {
        if (itemSources is null) return;
        if (bosses is null) return;
        if (itemId == 0 || !bosses.TryGetValue(fightNo, out var boss)) return;

        var bossNameRaw = Services.DataManager.GetExcelSheet<BNpcName>().GetRow(boss.BNpcNameId).Singular;
        if (bossNameRaw.IsEmpty) return;
        var bossNameText = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(bossNameRaw.ExtractText());
        ReadOnlySeString bossSource = $"Boss {fightNo + 1}: {bossNameText}";

        if (!itemSources.TryGetValue(itemId, out var sources)) {
            sources = [];
            itemSources[itemId] = sources;
        }
        if (!sources.Contains(bossSource)) {
            sources.Add(bossSource);
        }
    }

    private static List<T> LoadItems<T>(string resourceName) where T : ICsv, new() => CsvLoader.LoadResource<T>(
        resourceName: resourceName,
        includesHeaders: false,
        out _,
        out _,
        Services.DataManager.GameData,
        Services.DataManager.GameData.Options.DefaultExcelLanguage
    );

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
