using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Utility;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public class AddonConfig {
    private string fileName = null!;
   
    public static AddonConfig Load(string fileName, Keybind defaultKeyCombo) {
        var loadedConfig = Config.LoadConfig<AddonConfig>(fileName);
        loadedConfig.fileName = fileName;

        if (loadedConfig.Keybind is { Key: VirtualKey.NO_KEY, Modifiers.Count: 0 }) {
            loadedConfig.Keybind = defaultKeyCombo;
            loadedConfig.Save();
        }
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
