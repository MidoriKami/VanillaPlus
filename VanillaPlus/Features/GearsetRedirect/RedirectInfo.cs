using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit.Premade;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.GearsetRedirect;

public unsafe class RedirectInfo : IInfoNodeData {
    public required int AlternateGearsetId { get; init; }
    public required uint TerritoryType { get; init; }

    public string GetLabel()
        => GetGearsetData().NameString;

    public string GetSubLabel()
        => $"When in {Services.DataManager.GetExcelSheet<TerritoryType>().GetRow(TerritoryType).PlaceName.Value.Name}";

    public uint? GetId()
        => (uint) AlternateGearsetId;

    public uint? GetIconId()
        => GetGearsetData().ClassJob + 62000u;

    public string? GetTexturePath()
        => null;

    public int Compare(IInfoNodeData other, string sortingMode) => sortingMode switch {
        var s when s == Strings.SortOption_Alphabetical => string.CompareOrdinal(GetLabel(), other.GetLabel()),
        var s when s == Strings.SortOption_Id => GetId()?.CompareTo(other.GetId()) ?? 0,
        _ => 0,
    };
    
    private ref RaptureGearsetModule.GearsetEntry GetGearsetData()
        => ref RaptureGearsetModule.Instance()->Entries[AlternateGearsetId];
}
