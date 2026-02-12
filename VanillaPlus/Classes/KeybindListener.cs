using System;
using System.Diagnostics;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace VanillaPlus.Classes;

public unsafe class KeybindListener : IDisposable {
    private readonly Stopwatch debouncer = Stopwatch.StartNew();

    public required AddonConfig AddonConfig { get; set; }

    public delegate void KeybindCallbackDelegate(ref bool isHandled);
    
    public KeybindCallbackDelegate? KeybindCallback { get; set; }

    public KeybindListener()
        => Services.Framework.Update += OnFrameworkUpdate;

    public void Dispose()
        => Services.Framework.Update -= OnFrameworkUpdate;

    private void OnFrameworkUpdate(IFramework framework) {
        if (!AddonConfig.KeybindEnabled) return;
        if (AddonConfig.DisableInCombat && Services.Condition.IsBoundByDuty) return;

        // Don't process keybinds if we are settings up a new keybind
        if (PluginSystem.WindowSystem.Windows.Any(window => window.WindowName.Contains("Keybind Modal") && window.IsOpen)) return;

        // Don't process keybinds if any input text is active
        if (RaptureAtkModule.Instance()->IsTextInputActive()) return;

        if (AddonConfig.Keybind.IsPressed() && debouncer.ElapsedMilliseconds >= 25) {
            debouncer.Restart();

            var isHandled = false;
            KeybindCallback?.Invoke(ref isHandled);

            // If we handled this keypress, then suppress it.
            if (isHandled) {
                AddonConfig.Keybind.Reset();
            }
        }
    }
}
