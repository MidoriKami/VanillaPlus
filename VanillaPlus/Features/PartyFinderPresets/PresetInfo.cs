using KamiToolKit.Addons.Interfaces;

namespace VanillaPlus.Features.PartyFinderPresets;

public class PresetInfo : IInfoNodeData {
    public string Name { get; set; } = "NameNotInitialized";

    public string GetLabel() 
        => Name;

    public string? GetSubLabel()
        => null;

    public uint? GetId()
        => null;

    public uint? GetIconId() => 61483;

    public string? GetTexturePath()
        => null;

    public int Compare(IInfoNodeData other, string sortingMode)
        => string.CompareOrdinal(Name, (other as PresetInfo)?.Name);
}
