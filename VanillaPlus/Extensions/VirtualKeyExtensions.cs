using Dalamud.Game.ClientState.Keys;

namespace VanillaPlus.Extensions;

public static class VirtualKeyExtensions {
    extension(VirtualKey key) {
        public bool IsModifier => key is VirtualKey.MENU or VirtualKey.SHIFT or VirtualKey.CONTROL;
        public bool IsKey => !key.IsModifier;
    }
}
