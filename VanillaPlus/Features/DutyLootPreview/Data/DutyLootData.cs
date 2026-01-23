using System.Collections.Generic;

namespace VanillaPlus.Features.DutyLootPreview.Data;

/// <summary>
/// Immutable state representing the current duty loot data.
/// </summary>
public record DutyLootData {
    public static DutyLootData Empty(uint contentId) => new() {
        ContentId = contentId,
        Items = [],
    };

    public uint? ContentId { get; init; }

    public List<DutyLootItem> Items { get; init; } = [];
    public Dictionary<uint, DutyLootItem> ItemIndex { get; init; } = new();

    internal DutyLootItem? GetOrAddItem(uint itemId) {
        if (ItemIndex.TryGetValue(itemId, out var item)) {
            return item;
        }

        var newItem = DutyLootItem.FromItemId(itemId);
        if (newItem is null) return null;

        Items.Add(newItem);
        ItemIndex.Add(itemId, newItem);
        return newItem;
    }
}
