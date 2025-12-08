using System.Numerics;
using Dalamud.Utility;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public class AddonConfig {
    private string fileName = null!;
   
    public static AddonConfig Load(string fileName) {
        var loadedConfig = Config.LoadConfig<AddonConfig>(fileName);
        loadedConfig.fileName = fileName;

        return loadedConfig;
    }

    public void Save() {
        if (fileName.IsNullOrEmpty()) return;
        Config.SaveConfig(this, fileName);
    }

    public Vector2 WindowSize = Vector2.Zero;
    public bool KeybindEnabled = true;

    public Keybind Keybind = new();
}
