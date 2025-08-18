using System.Collections.Generic;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Extensions;

public static class KeyStateExtensions {
    public static bool IsKeybindPressed(this IKeyState keyState, IEnumerable<VirtualKey> keys) {
        foreach (var key in keys) {
            if (!keyState[(int)key]) {
                return false;
            }
        }

        return true;
    }
    
    public static void ResetKeyCombo(this IKeyState keyState, IEnumerable<VirtualKey> keys) {
        foreach(var key in keys){
            if (key is VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT) continue;
            
            keyState[(int)key] = false;
        }
    }
}
