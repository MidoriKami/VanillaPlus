using VanillaPlus.Enums;

namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarningSetting {
    public uint ItemId;
    public WarningMode Mode = WarningMode.Above;
    public int Limit;
}
