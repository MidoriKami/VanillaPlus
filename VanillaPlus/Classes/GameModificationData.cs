using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public abstract class GameModificationData<T> where T : GameModificationData<T>, new() {
    protected abstract string FileName { get; }

    public static T Load() {
        var configFileName = new T().FileName;
        
        Services.PluginLog.Debug($"Loading Data {configFileName}.data.json");
        return Data.LoadData<T>($"{configFileName}.data.json");
    } 
    
    public void Save() {
        Services.PluginLog.Debug($"Saving Data {FileName}.data.json");
        Data.SaveData(this, $"{FileName}.data.json");
    }
}
