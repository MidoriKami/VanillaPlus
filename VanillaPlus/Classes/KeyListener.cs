using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Classes;

public class KeyListener : IDisposable {

    private readonly Dictionary<VirtualKey, bool> virtualKeyMap = [];

    public KeyListener() {
        foreach (var value in Services.GetService<IKeyState>().GetValidVirtualKeys()) {
            virtualKeyMap.TryAdd(value, false);
        }

        Services.GetService<IFramework>().Update += OnFrameworkUpdate;
    }

    public void Dispose()
        => Services.GetService<IFramework>().Update -= OnFrameworkUpdate;

    private void OnFrameworkUpdate(IFramework framework) {
        foreach (var pair in virtualKeyMap) {
            var newState = Services.GetService<IKeyState>()[(int)pair.Key];

            if (virtualKeyMap[pair.Key] != newState) {
                OnKeyPressed?.Invoke(pair.Key, newState);
            }

            virtualKeyMap[pair.Key] = newState;
        }
    }

    public Action<VirtualKey, bool>? OnKeyPressed { get; set; }
}
