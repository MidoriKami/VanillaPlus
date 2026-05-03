using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

/// <summary>
/// For use with per-character settings. This will save a file in a character specific directory.
/// </summary>
/// <remarks>You must be logged in to load or save a character config.</remarks>
public abstract class GameModificationCharacterConfig<T> where T : GameModificationCharacterConfig<T>, new() {
    protected abstract string FileName { get; }

    public static T Load() {
        var fileName = new T().FileName;
        Services.PluginLog.Debug($"Loading Character Config {fileName}");

        return Config.LoadCharacterConfig<T>(fileName);
    }

    public void Save() {
        Services.PluginLog.Debug($"Saving Character Config {FileName}");
        Config.SaveCharacterConfig(this, FileName);
    }
}
