using System.Threading.Tasks;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public abstract class GameModificationData<T> where T : GameModificationData<T>, new() {
    protected abstract string FileName { get; }

    public static async Task<T> Load() {
        var configFileName = new T().FileName;

        Services.PluginLog.InternalDebug($"Loading Data {configFileName}.data.json");
        return await Data.LoadData<T>($"{configFileName}.data.json");
    }

    public async Task Save() {
        Services.PluginLog.InternalDebug($"Saving Data {FileName}.data.json");
        await Data.SaveData(this, $"{FileName}.data.json");
    }
}
