using System;
using System.Text.RegularExpressions;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit.Premade.GenericListItemNodes;

namespace VanillaPlus.Features.GearsetRedirect.Nodes;

public unsafe class GearsetInfo {
    public required int GearsetId;

    public static int Comparer(GearsetInfo left, GearsetInfo right, string mode) {
        if (mode == Strings.GearsetRedirect_SortAlphabetical) return string.Compare(GetGearsetData(left.GearsetId).NameString, GetGearsetData(right.GearsetId).NameString, StringComparison.OrdinalIgnoreCase);
        else if (mode == Strings.GearsetRedirect_SortId) return left.GearsetId.CompareTo(right.GearsetId);
        return 0;
    }
    
    public static bool IsMatch(GearsetInfo item, string searchString) {
        var regex = new Regex(searchString, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        
        return regex.IsMatch(GetGearsetData(item.GearsetId).NameString);
    }
    
    private static ref RaptureGearsetModule.GearsetEntry GetGearsetData(int gearsetId)
        => ref RaptureGearsetModule.Instance()->Entries[gearsetId];
}

public unsafe class GearsetInfoListItemNode : GenericListItemNode<GearsetInfo> {
    protected override uint GetIconId(GearsetInfo data)
        => data.GearsetId < 0 ? 60072 : GetGearsetData(data.GearsetId)->ClassJob + 62000u;

    protected override string GetLabelText(GearsetInfo data)
        => data.GearsetId < 0 ? "Nothing Selected" : GetGearsetData(data.GearsetId)->NameString;

    protected override string GetSubLabelText(GearsetInfo data)
        => data.GearsetId < 0 ? string.Empty : $"{SeIconChar.ItemLevel.ToIconString()} {GetGearsetData(data.GearsetId)->ItemLevel}";

    protected override uint? GetId(GearsetInfo data)
        => data.GearsetId < 0 ? null : (uint) data.GearsetId;

    private static RaptureGearsetModule.GearsetEntry* GetGearsetData(int gearsetId)
        => RaptureGearsetModule.Instance()->GetGearset(gearsetId);
}
