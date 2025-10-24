using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;

namespace VanillaPlus.Classes;

public class Keybind {
    public VirtualKey Key { get; set; } = VirtualKey.NO_KEY;
    public HashSet<VirtualKey> Modifiers { get; set; } = [];

    public bool IsPressed()
        => Services.KeyState.IsKeybindPressed(Modifiers.Append(Key));

    public void Reset()
        => Services.KeyState.ResetKeyCombo([ Key ] );

    public override string ToString()
        => string.Join(" + ", Modifiers.Append(Key));
}
