using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Classes;

public class Keybind {
    public VirtualKey Key { get; init; } = VirtualKey.NO_KEY;
    public HashSet<VirtualKey> Modifiers { get; init; } = [];

    public bool IsPressed()
        => Key is not VirtualKey.NO_KEY && Service<IKeyState>.Get().IsKeybindPressed(Modifiers.Append(Key));

    public void Reset()
        => Service<IKeyState>.Get().ResetKeyCombo([Key]);

    public override string ToString()
        => string.Join(" + ", Modifiers.Append(Key));
}
