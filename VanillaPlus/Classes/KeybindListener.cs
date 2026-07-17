using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace VanillaPlus.Classes;

/// <summary>
/// Generalized implementation of a Keybind Listener.
/// </summary>
public class KeybindListener {

    /// <summary>
    /// Callback delegate, isHandled will cause the keys input to be reset preventing other keybind listeners from triggering.
    /// </summary>
    public delegate void KeybindCallbackDelegate(ref bool isHandled);

    /// <summary>
    /// Gets or sets if this listener is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The keybind to be scanning for.
    /// </summary>
    public Keybind Keybind { get; set; } = new();

    /// <summary>
    /// The function that is called when the keybind is pressed.
    /// </summary>
    public KeybindCallbackDelegate? Callback { get; set; }

    /// <summary>
    /// Gets or sets if this keybind should be disabled in combat.
    /// </summary>
    /// <remarks>
    /// By default, this is enabled, thus disabling keybinds in combat.
    /// </remarks>
    public bool DisableInCombat { get; set; } = true;

    /// <summary>
    /// How long to wait before allowing another keybind press to trigger the callback.
    /// </summary>
    public int DebounceMilliseconds { get; set; }

    /// <summary>
    /// Updates the keybind check, this reads dalamud's <see cref="IKeyState"/> service, and is slightly out of sync with the game.
    /// </summary>
    public unsafe void Update() {
        if (!IsEnabled) return;
        if (DisableInCombat && Services.GetService<ICondition>().IsInCombat) return;

        // Don't process keybinds if any input text is active
        if (RaptureAtkModule.Instance()->IsTextInputActive()) return;

        if (Keybind.IsPressed() && debouncer.ElapsedMilliseconds >= DebounceMilliseconds) {
            debouncer.Restart();

            var isHandled = false;
            Callback?.Invoke(ref isHandled);

            // If we handled this keypress, then suppress it.
            if (isHandled) {
                Keybind.Reset();
            }
        }
    }

    private readonly Stopwatch debouncer = Stopwatch.StartNew();
}
