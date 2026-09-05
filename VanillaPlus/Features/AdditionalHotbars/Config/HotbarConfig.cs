using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

namespace VanillaPlus.Features.AdditionalHotbars.Config;

public class HotbarConfig {
    public string Name { get; set; } = string.Empty;

    public int Width { get; set; } = 12;
    public int Height { get; set; } = 1;

    public int VerticalSpacing { get; set; } = 4;
    public int HorizontalSpacing { get; set; }

    public Vector2? Position { get; set; }

    public bool IsEnabled { get; set; } = true;

    public uint LinkedClassJob { get; set; }

    [JsonIgnore]
    public bool MovingEnabled { get; set; }

    [JsonIgnore]
    public bool NeedsRebuildLayout { get; set; } = true;

    [JsonIgnore]
    public bool NeedsRecalcLayout { get; set; }

    public List<SlotData> Slots { get; set; } = List<SlotData>.CreateInitialized(12);
}
