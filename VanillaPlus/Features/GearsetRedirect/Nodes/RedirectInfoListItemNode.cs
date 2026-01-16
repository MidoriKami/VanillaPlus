using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit.Premade.GenericListItemNodes;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.GearsetRedirect.Nodes;

public unsafe class RedirectInfo {
    public required int AlternateGearsetId;
    public required uint TerritoryType;

    public static bool IsMatch(RedirectInfo item, string searchString) {
        var regex = new Regex(searchString, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        if (regex.IsMatch(GetGearsetData(item.AlternateGearsetId).NameString)) return true;
        
        var territoryInfo = Services.DataManager.GetExcelSheet<TerritoryType>().GetRow(item.TerritoryType);
        if (regex.IsMatch(territoryInfo.PlaceName.ValueNullable?.Name.ToString() ?? string.Empty)) return true;

        return false;
    }

    private static ref RaptureGearsetModule.GearsetEntry GetGearsetData(int gearsetId)
        => ref RaptureGearsetModule.Instance()->Entries[gearsetId];
}


public unsafe class RedirectInfoListItemNode : GenericListItemNode<RedirectInfo> {
    protected override uint GetIconId(RedirectInfo data)
        => GetGearsetData(data.AlternateGearsetId).ClassJob + 62000u;

    protected override string GetLabelText(RedirectInfo data)
        => GetGearsetData(data.AlternateGearsetId).NameString;

    protected override string GetSubLabelText(RedirectInfo data)
        => $"When in {Services.DataManager.GetExcelSheet<TerritoryType>().GetRow(data.TerritoryType).PlaceName.Value.Name}";

    protected override uint? GetId(RedirectInfo data)
        => (uint) data.AlternateGearsetId;
    
    private static ref RaptureGearsetModule.GearsetEntry GetGearsetData(int gearsetId)
        => ref RaptureGearsetModule.Instance()->Entries[gearsetId];
}
