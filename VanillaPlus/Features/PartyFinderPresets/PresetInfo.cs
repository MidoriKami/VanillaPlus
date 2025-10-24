using KamiToolKit.Addons.Interfaces;

namespace VanillaPlus.Features.PartyFinderPresets;

public class PresetInfo : IInfoNodeData {
    public string Name { get; set; } = "NameNotInitialized";

    public string GetLabel() => Name;

    public uint? GetIconId() => 61483;

    public int Compare(IInfoNodeData other, string sortingMode)
        => string.CompareOrdinal(Name, (other as PresetInfo)?.Name);
}
