using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using VanillaPlus.Extensions;

namespace VanillaPlus.Classes;

public class KeybindListener : IDisposable {
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
        
        if (Services.KeyState.IsKeybindPressed(KeyCombo) && debouncer.ElapsedMilliseconds >= 250) {
            Services.KeyState.ResetKeyCombo(KeyCombo);
            debouncer.Restart();
            
            KeybindCallback?.Invoke();
        }
    }
}
