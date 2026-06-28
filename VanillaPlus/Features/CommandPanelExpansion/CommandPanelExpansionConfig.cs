using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.CommandPanelExpansion;

public class CommandPanelExpansionConfig : GameModificationConfig<CommandPanelExpansionConfig> {
    protected override string FileName => "CommandPanelExpansion";

    // The native Command Panel ships with 10 page-tab radio buttons (node ids 7..16),
    // but only the first 4 are shown by default. This controls how many to reveal (4..9).
    public int PageCount = 4;

    // Pages 5+ (0-based index 4+) may contain corrupted native slot data. When disabled, the
    // native DragDrop slots are hidden on those pages so they cannot be edited.
    public bool UseNativeElementsForPages5Plus;

    // Total grid dimensions. The native panel is 5x5; anything above that is filled
    // with extra plugin-drawn slot squares.
    public int Columns = 5;
    public int Rows = 5;

    // Persisted contents of the extra (plugin-drawn) slots. The game does not store these, so we
    // keep them here in the plugin's config - separately for each character, keyed by content id.
    public Dictionary<ulong, List<SavedSlotCommand>> CharacterSlots = [];

    // Layout shared by all characters while the Command Panel Sync modification is enabled. Reads
    // come from here and every write is mirrored onto it (and onto each per-character entry), so all
    // characters are overwritten with the current character's slots.
    public List<SavedSlotCommand> SharedSlots = [];

    // Legacy single global slot list from before per-character storage existed. Migrated into
    // CharacterSlots for the first character that opens the panel, then left empty.
    public List<SavedSlotCommand> SavedSlots = [];
}

public class SavedSlotCommand {
    // The Command Panel page (0-based tab index) this slot belongs to.
    public int Page;
    public int Column;
    public int Row;

    // RaptureHotbarModule.HotbarSlotType stored as its underlying byte value.
    public byte CommandType;
    public uint CommandId;
}
