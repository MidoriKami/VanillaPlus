using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.WindowBackground;

public class WindowBackgroundConfig : GameModificationConfig<WindowBackgroundConfig> {
    protected override string FileName => "WindowBackground";

    public override int Version => 1;

    public List<WindowBackgroundSetting> Settings = [
        new() { AddonName = "_ToDoList" },
    ];

    public WindowBackgroundSetting GetSettings(string addonName)
        => Settings.First(setting => setting.AddonName == addonName);

    protected override bool TryMigrateConfig(int? fileVersion, JObject jObject) {
        switch (fileVersion) {
            case null or 0:
                if (jObject["Settings"] is not JArray settingsArray) return false;

                foreach (var settingToken in settingsArray) {

                    if (settingToken is not JObject setting) continue;

                    if (setting["Padding"] is JObject oldPadding) {
                        var x = oldPadding["X"]?.Value<float>() ?? 15.0f;
                        var y = oldPadding["Y"]?.Value<float>() ?? 15.0f;

                        setting["PaddingVector"] = new JObject {
                            ["X"] = x,
                            ["Y"] = y,
                            ["Z"] = x,
                            ["W"] = y,
                        };

                        // Remove old property key
                        setting.Remove("Padding");
                    }
                }

                return true;
        }

        return false;
    }
}
