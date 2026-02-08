using System.ComponentModel;

namespace VanillaPlus.Features.MSQProgressPercent;

public enum MSQProgressBarMode {
    [Description("Entire Game")]
    EntireGame,
    
    [Description("Expansion")]
    Expansion,

    // [Description("Chapter")]
    // Chapter,
}
