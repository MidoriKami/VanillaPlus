using System.Numerics;
using System.Threading.Tasks;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public class AddonConfig {
    private string fileName = null!;

    public static async Task<AddonConfig> Load(string fileName) {
        var loadedConfig = await Config.LoadConfig<AddonConfig>(fileName);
        loadedConfig.fileName = fileName;

        return loadedConfig;
    }

    public async Task Save() {
        if (fileName.IsNullOrEmpty()) return;
        await Config.SaveConfig(this, fileName);
    }

    public Vector2 WindowSize = Vector2.Zero;
    public bool KeybindEnabled = true;
    public bool DisableInCombat = true;

    public Keybind Keybind = new();
}
