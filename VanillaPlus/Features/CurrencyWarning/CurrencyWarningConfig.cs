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

    public void Migrate() {
        bool needsSave = false;

        foreach (var setting in WarningSettings) {
            if (setting.OldSettings == null) continue;

            bool TryGetBool(string key) => setting.OldSettings!.TryGetValue(key, out var val) && val.GetBoolean();
            int TryGetInt(string key) => setting.OldSettings!.TryGetValue(key, out var val) ? val.GetInt32() : 0;

            if (setting.OldSettings.ContainsKey("EnableHighLimit") ||
                setting.OldSettings.ContainsKey("EnableLowLimit")) {

                bool oldEnableHigh = TryGetBool("EnableHighLimit");
                bool oldEnableLow = TryGetBool("EnableLowLimit");
                int oldHighLimit = TryGetInt("HighLimit");
                int oldLowLimit = TryGetInt("LowLimit");

                if (oldEnableHigh) {
                    setting.Mode = WarningMode.Above;
                    setting.Limit = oldHighLimit;
                } else if (oldEnableLow) {
                    setting.Mode = WarningMode.Below;
                    setting.Limit = oldLowLimit;
                } else {
                    setting.Mode = WarningMode.Above;
                    setting.Limit = oldHighLimit != 0 ? oldHighLimit : oldLowLimit;
                }

                setting.OldSettings = null;
                needsSave = true;
            }
        }

        if (needsSave) {
            Save();
        }
    }
}
