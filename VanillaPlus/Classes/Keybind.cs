using System.Collections.Generic;
using Dalamud.Game.ClientState.Keys;

namespace VanillaPlus.Classes;

public class Keybind {
    public VirtualKey Key { get; set; } = VirtualKey.NO_KEY;
    public HashSet<VirtualKey> Modifiers { get; set; } = [];
}
