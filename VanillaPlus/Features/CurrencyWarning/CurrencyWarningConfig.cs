using System.Numerics;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarningConfig : GameModificationConfig<CurrencyWarningConfig> {
    protected override string FileName => "CurrencyWarning";

    public Vector2 Position = Vector2.Zero;
    public float Scale = 1.0f;
    public bool IsMoveable = true;
}
