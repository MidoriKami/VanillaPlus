using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using CabinetSheet = Lumina.Excel.Sheets.Cabinet;

namespace VanillaPlus.Features.DutyLootPreview.Data;

public class DutyLootItem : IComparable {
    public required uint ItemId { get; init; }
    public required uint IconId { get; init; }
    public ReadOnlySeString Name { get; private init; }
    public required byte FilterGroup { get; init; }
    public required byte OrderMajor { get; init; }
    public required byte OrderMinor { get; init; }
    public required bool IsUnlockable { get; init; }
    public required bool IsUnlocked { get; init; }
    public required bool CanTryOn { get; init; }
    public required List<ReadOnlySeString> Sources { get; init; }

    private static readonly Lazy<FrozenDictionary<uint, uint>> CabinetLookup = new(()
        => Services.DataManager.Excel.GetSheet<CabinetSheet>()
            .Where(row => row.RowId >= 1048 && row.Item.RowId != 0)
            .ToFrozenDictionary(row => row.Item.RowId, row => row.RowId));

    public static DutyLootItem? FromItemId(uint itemId) {
        var item = Services.DataManager.GetItem(itemId);
        if (item.Icon is 0 || item.Name.IsEmpty) return null;

        return new DutyLootItem {
            ItemId = item.RowId,
            IconId = item.Icon,
            Name = item.Name,
            FilterGroup = item.FilterGroup,
            OrderMajor = item.ItemUICategory.ValueNullable?.OrderMajor ?? 0,
            OrderMinor = item.ItemUICategory.ValueNullable?.OrderMinor ?? 0,
            IsUnlockable = IsItemUnlockable(item),
            IsUnlocked = IsItemUnlocked(item),
            CanTryOn = CheckCanTryOn(item),
            Sources = [],
        };
    }

    public bool IsEquipment 
        => FilterGroup is 1 or 2 or 3 or 4 or 45;

    private static bool IsItemUnlockable(Item item) {
        if (Services.UnlockState.IsItemUnlockable(item))
            return true;

        return CabinetLookup.Value.ContainsKey(item.RowId);
    }

    private static unsafe bool IsItemUnlocked(Item item) {
        if (Services.UnlockState.IsItemUnlockable(item))
            return Services.UnlockState.IsItemUnlocked(item);

        var itemFinderModule = ItemFinderModule.Instance();

        // check against Cabinet cache
        var cabinetCacheState = Marshal.ReadByte((nint)itemFinderModule + 0xA9);
        if (cabinetCacheState == 2  && CabinetLookup.Value.TryGetValue(item.RowId, out var cabinetRowId)) {
            (var byteIndex, var bitOffset) = Math.DivRem(cabinetRowId - 1048, 32);
            if (itemFinderModule->CabinetItemUnlockBits.Length >= byteIndex)
                return (itemFinderModule->CabinetItemUnlockBits[(int)byteIndex] & (1 << (int)bitOffset)) != 0;
        }

        return false;
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
        if (race is 0) return false;

        return true;
    }

    public int CompareTo(object? other) {
        if (other is not DutyLootItem otherItem) return 1;

        // Sort by category: Misc+Unlockable > Misc > Equipment
        var categoryResult = GetCategoryPriority().CompareTo(otherItem.GetCategoryPriority());
        if (categoryResult != 0) return categoryResult;

        var result = -OrderMajor.CompareTo(otherItem.OrderMajor);
        if (result != 0) return result;

        result = -OrderMinor.CompareTo(otherItem.OrderMinor);
        if (result != 0) return result;

        return string.Compare(Name.ToString(), otherItem.Name.ToString(), StringComparison.Ordinal);
    }

    private int GetCategoryPriority() {
        if (!IsEquipment && IsUnlockable) return 0; // Misc + Unlockable (highest)
        if (!IsEquipment) return 1;                 // Misc
        return 2;                                   // Equipment (lowest)
    }
}
