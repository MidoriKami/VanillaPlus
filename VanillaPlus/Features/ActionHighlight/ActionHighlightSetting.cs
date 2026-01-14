using System.Text.Json.Serialization;

namespace VanillaPlus.Features.ActionHighlight;

public class ActionHighlightSetting {
    public bool IsEnabled;
    public uint ActionId;
    public int ThresholdMs = 3000;

    [JsonIgnore] public ActionHighlightConfig? ParentConfig;
}
