using System;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Classes;

/// <summary>
/// Keybind listener used to open or close an addon.
/// </summary>
public class AddonKeybindListener : KeybindListener, IDisposable {

    public required AddonConfig AddonConfig {
        get;
        set {
            field = value;
            Keybind = value.Keybind;
            IsEnabled = true;
        }
    }

    public AddonKeybindListener()
        => Services.Framework.Update += OnFrameworkUpdate;

    public void Dispose()
        => Services.Framework.Update -= OnFrameworkUpdate;

    private void OnFrameworkUpdate(IFramework framework)
        => Update();
}
