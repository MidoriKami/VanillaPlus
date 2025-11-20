using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit.Premade;

namespace VanillaPlus.Features.GearsetRedirect;

public unsafe class GearsetInfo : IInfoNodeData {
    
    public required int GearsetId { get; init; }

    public string GetLabel()
        => GearsetId < 0 ? "Nothing Selected" : GetGearsetData().NameString;

    public string GetSubLabel()
        => GearsetId < 0 ? string.Empty : $"{SeIconChar.ItemLevel.ToIconString()} {GetGearsetData().ItemLevel}";

    public uint? GetId()
        => GearsetId < 0 ? null : (uint) GearsetId;

    public uint? GetIconId()
        => GearsetId < 0 ? 60072 : GetGearsetData().ClassJob + 62000u;

    public string? GetTexturePath()
        => null;

    public int Compare(IInfoNodeData other, string sortingMode) => sortingMode switch {
        "Alphabetical" => string.CompareOrdinal(GetLabel(), other.GetLabel()),
        "Id" => GetId()?.CompareTo(other.GetId()) ?? 0,
        _ => 0,
    };

    private ref RaptureGearsetModule.GearsetEntry GetGearsetData()
        => ref RaptureGearsetModule.Instance()->Entries[GearsetId];
}
