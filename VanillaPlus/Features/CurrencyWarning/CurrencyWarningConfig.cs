using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud.Interface;
using Newtonsoft.Json.Linq;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

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

    protected override bool TryMigrateConfig(int? fileVersion, JObject jObject) {
        switch (fileVersion) {
            case null:
                WarningSettings = jObject["WarningSettings"]?.Select(ParseOldWarningSetting).ToList() ?? []; 
                return true;
        }

        return false;
    }

    private static CurrencyWarningSetting ParseOldWarningSetting(JToken token) {
        var enableLowLimit = token["EnableLowLimit"]?.ToObject<bool>() ?? false;
        var enableHighLimit = token["EnableHighLimit"]?.ToObject<bool>() ?? false;
        var lowLimit = token["LowLimit"]?.ToObject<int>() ?? 0;
        var highLimit = token["HighLimit"]?.ToObject<int>() ?? 0;
        var itemId = token["ItemId"]?.ToObject<uint>() ?? 0;

        return new CurrencyWarningSetting {
            ItemId = itemId,
            Mode = enableHighLimit ? WarningMode.Above : enableLowLimit ? WarningMode.Below : WarningMode.Above,
            Limit = enableHighLimit ? highLimit : enableLowLimit ? lowLimit : highLimit,
        };
    }
}
