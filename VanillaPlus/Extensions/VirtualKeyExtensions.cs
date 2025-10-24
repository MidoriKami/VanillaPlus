using Dalamud.Game.ClientState.Keys;

namespace VanillaPlus.Extensions;

public static class VirtualKeyExtensions {
    public static bool IsModifier(this VirtualKey key)
        => key is VirtualKey.MENU or VirtualKey.SHIFT or VirtualKey.CONTROL;

    public static bool IsKey(this VirtualKey key)
        => !key.IsModifier();
}
