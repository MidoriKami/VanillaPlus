using System.Numerics;

namespace VanillaPlus.Features.CurrencyOverlay;

public class CurrencySettings {
    public uint ItemId;
    public Vector2 Position = Vector2.Zero;
    public bool EnableLowLimit;
    public bool EnableHighLimit;
    public int LowLimit;
    public int HighLimit;
    public bool IconReversed;
}
