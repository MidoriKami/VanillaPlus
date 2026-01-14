namespace VanillaPlus.Features.ActionHighlight;

public class ActionHighlightSetting {
    public uint ActionId { get; set; }
    public int ThresholdMs { get; set; } = 3000;
}
