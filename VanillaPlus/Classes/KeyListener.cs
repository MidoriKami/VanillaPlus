using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Classes;

public class KeyListener : IDisposable {

    private readonly Dictionary<VirtualKey, bool> virtualKeyMap = [];

    public KeyListener() {
        foreach (var value in IKeyState.Get().GetValidVirtualKeys()) {
            virtualKeyMap.TryAdd(value, false);
        }

        IFramework.Get().Update += OnFrameworkUpdate;
    }

    public void Dispose()
        => IFramework.Get().Update -= OnFrameworkUpdate;

    private void OnFrameworkUpdate(IFramework framework) {
        foreach (var pair in virtualKeyMap) {
            var newState = IKeyState.Get()[(int)pair.Key];

            if (virtualKeyMap[pair.Key] != newState) {
                OnKeyPressed?.Invoke(pair.Key, newState);
            }

            virtualKeyMap[pair.Key] = newState;
        }
    }

    public Action<VirtualKey, bool>? OnKeyPressed { get; set; }
}
