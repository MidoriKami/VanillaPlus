﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace VanillaPlus.Classes;

public unsafe class KeybindListener : IDisposable {
    private readonly Stopwatch debouncer = Stopwatch.StartNew();
    
    public required HashSet<VirtualKey> KeyCombo { get; set; }
    public required Action? KeybindCallback { get; init; }
    
    public KeybindListener()
        => Services.Framework.Update += OnFrameworkUpdate;

    public void Dispose()
        => Services.Framework.Update -= OnFrameworkUpdate;

    private void OnFrameworkUpdate(IFramework framework) {
        // Don't process keybinds if we are settings up a new keybind
        if (System.WindowSystem.Windows.Any(window => window.WindowName.Contains("Keybind Modal") && window.IsOpen)) return;

        // Don't process keybinds if any input text is active
        if (RaptureAtkModule.Instance()->IsTextInputActive()) return;
        
        if (Services.KeyState.IsKeybindPressed(KeyCombo) && debouncer.ElapsedMilliseconds >= 250) {
            Services.KeyState.ResetKeyCombo(KeyCombo);
            debouncer.Restart();
            
            KeybindCallback?.Invoke();
        }
    }
}
