using System.ComponentModel;

namespace VanillaPlus.Features.CurrencyWarning;

public enum WarningMode {
    [Description("Warn when Above")]
    Above,
    
    [Description("Warn when Below")]
    Below,
}
