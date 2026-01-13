using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit.Premade.GenericSearchListItemNodes;

namespace VanillaPlus.NativeElements.SearchResultNodes;

public class GearsetListItemNode : GenericListItemNode<RaptureGearsetModule.GearsetEntry> {
    protected override uint GetIconId(RaptureGearsetModule.GearsetEntry data)
        => data.ClassJob + 62000u;
    
    protected override string GetLabelText(RaptureGearsetModule.GearsetEntry data)
        => data.NameString;

    protected override string GetSubLabelText(RaptureGearsetModule.GearsetEntry data)
        => $"{SeIconChar.ItemLevel.ToIconString()} {data.ItemLevel}";

    protected override uint? GetId(RaptureGearsetModule.GearsetEntry data)
        => data.Id;
}
