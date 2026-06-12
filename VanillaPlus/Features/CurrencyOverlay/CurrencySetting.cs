using System.Numerics;
using System.Text.Json.Serialization;

namespace VanillaPlus.Features.CurrencyOverlay;

public class CurrencySetting {
    public uint ItemId;
    public Vector2 Position = Vector2.Zero;
    public bool EnableLowLimit;
    public bool EnableHighLimit;
    public int LowLimit;
    public int HighLimit;
    public bool IconReversed;
    public bool TextReversed;
    public float Scale = 1.0f;
    public float FadePercent;
    public bool FadeIfNoWarnings;

    [JsonIgnore] public bool IsNodeMoveable;
}
