using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using VanillaPlus.Utilities;

namespace VanillaPlus.Classes;

public abstract class GameModificationData<T> where T : GameModificationData<T>, new() {
    protected abstract string FileName { get; }

    public static async Task<T> Load() {
        var configFileName = new T().FileName;

        IPluginLog.Get().Debug($"Loading Data {configFileName}.data.json");
        return await Data.LoadData<T>($"{configFileName}.data.json");
    }

    private readonly SemaphoreSlim saveLock = new(1, 1);
    private bool pendingSave;

    public Task Save() {
        IPluginLog.Get().Verbose($"Queuing Save for {FileName}.data.json");

        Interlocked.Exchange(ref pendingSave, true);

        return Task.Run(SaveAsync);
    }

    private async Task SaveAsync() {
        if (!await saveLock.WaitAsync(0)) {
            IPluginLog.Get().Verbose($"Save in progress for {FileName}.data.json, skipping save.");
            return;
        }

        try {
            while (Interlocked.Exchange(ref pendingSave, false)) {
                IPluginLog.Get().Debug($"Saving Data {FileName}.data.json");
                await Data.SaveData(this, $"{FileName}.data.json");
            }
        }
        catch (Exception e) {
            IPluginLog.Get().Error(e, $"Failed to save {FileName}.data.json");
        }
        finally {
            saveLock.Release();
        }
    }
}
