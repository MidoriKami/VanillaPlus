using System.Collections.Generic;
using System.Linq;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.WindowBackground;

public class WindowBackgroundConfig : GameModificationConfig<WindowBackgroundConfig> {
    protected override string FileName => "WindowBackground";

    public List<WindowBackgroundSetting> Settings = [
        new() { AddonName = "_ToDoList" },
    ];

    public WindowBackgroundSetting GetSettings(string addonName)
        => Settings.First(setting => setting.AddonName == addonName);
}
