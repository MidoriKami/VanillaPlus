using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Features.AdditionalHotbars.Config;

/// <summary>
/// Data representing a HotbarSlot and its set action or whatever.
/// </summary>
public class SlotData {
    public DragDropType DragDropType { get; set; }

    public RaptureHotbarModule.HotbarSlotType HotbarSlotType
        => UIGlobals.GetHotbarSlotTypeFromDragDropType(DragDropType);

    public uint Id { get; set; }
}
