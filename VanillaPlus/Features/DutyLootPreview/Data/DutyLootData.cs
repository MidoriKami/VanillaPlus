using System.Collections.Generic;

namespace VanillaPlus.Features.DutyLootPreview.Data;

/// <summary>
/// Immutable state representing the current duty loot data.
/// </summary>
public record DutyLootData {
    public static readonly DutyLootData Empty = new();

    public bool IsLoading { get; init; }
    public uint? ContentId { get; init; }
    public IReadOnlyList<DutyLootItem> Items { get; init; } = [];
}
