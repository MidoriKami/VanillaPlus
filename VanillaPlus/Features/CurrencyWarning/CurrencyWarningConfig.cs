using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud.Interface;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarningConfig : GameModificationConfig<CurrencyWarningConfig> {
    protected override string FileName => "CurrencyWarning";

    public bool IsConfigured = false;
    public Vector2 Position = Vector2.Zero;
    public float Scale = 1.0f;
    public uint LowIcon = 60073u;
    public uint HighIcon = 60074u;
    public Vector4 LowColor = KnownColor.Yellow.Vector();
    public Vector4 HighColor = KnownColor.OrangeRed.Vector();
    public bool PlayAnimations = true;

    public List<CurrencyWarningSetting> WarningSettings = [];
    
    [JsonIgnore] public bool IsMoveable = false;
}
