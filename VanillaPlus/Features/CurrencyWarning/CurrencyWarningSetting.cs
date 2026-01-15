namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarningSetting {
    public uint ItemId;
    public WarningMode Mode = WarningMode.Above;
    public int Limit;

    // public string GetLabel()
    //     => ItemId == 0 ? "Unknown" : Services.DataManager.GetItem(ItemId).Name.ToString();
    //
    // public string GetSubLabel()
    //     => "";
    //
    // public uint? GetIconId()
    //     => Services.DataManager.GetItem(ItemId).Icon;
    //
    // public uint? GetId()
    //     => ItemId;
    //
    // public string? GetTexturePath()
    //     => null;
    //
    // public int Compare(IInfoNodeData other, string sortingMode) {
    //     return string.CompareOrdinal(GetLabel(), other.GetLabel());
    // }
}
