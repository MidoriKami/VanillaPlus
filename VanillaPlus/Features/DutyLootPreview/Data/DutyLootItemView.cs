namespace VanillaPlus.Features.DutyLootPreview.Data;

/// <summary>
/// View model for displaying a duty loot item in the list.
/// Combines the item data with UI state (favorite status) and provides
/// the config reference so the node can handle interactions directly.
/// </summary>
public record DutyLootItemView(
    DutyLootItem Item,
    bool IsFavorite,
    DutyLootPreviewConfig Config
);
