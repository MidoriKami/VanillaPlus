﻿using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public abstract class GameModificationData<T> where T : GameModificationData<T>, new() {
    protected abstract string FileName { get; }
    public static T Load() {
        var configFileName = new T().FileName;
        
        Services.PluginLog.Debug($"Loading Data {configFileName}");
        return Data.LoadData<T>(configFileName);
    } 
    
    public void Save() {
        Services.PluginLog.Debug($"Saving Data {FileName}");
        Data.SaveData(this, FileName);
    }
}
