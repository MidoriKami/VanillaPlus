using System.Collections.Generic;
using System.Numerics;
using KamiToolKit.Classes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarningConfig : GameModificationConfig<CurrencyWarningConfig> {
    protected override string FileName => "CurrencyWarning";

    public Vector2 Position = Vector2.Zero;
    public float Scale = 1.0f;
    public bool IsMoveable = true;
    public uint LowIcon = 60073u;
    public uint HighIcon = 60074u;
    public Vector4 LowColor = ColorHelper.GetColor(25);
    public Vector4 HighColor = ColorHelper.GetColor(17);

    public List<CurrencyWarningSetting> WarningSettings = [];
}
