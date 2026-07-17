using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

/// <summary>
/// For use with per-character settings. This will save a file in a character specific directory.
/// </summary>
/// <remarks>You must be logged in to load or save a character config.</remarks>
public abstract class GameModificationCharacterConfig<T> where T : GameModificationCharacterConfig<T>, new() {
    protected abstract string FileName { get; }

    public static async Task<T> Load() {
        var fileName = new T().FileName;
        Service<IPluginLog>.Get().Debug($"Loading Character Config {fileName}");

        return await Config.LoadCharacterConfig<T>(fileName);
    }

    public async Task Save() {
        Service<IPluginLog>.Get().Debug($"Saving Character Config {FileName}");
        await Config.SaveCharacterConfig(this, FileName);
    }
}
