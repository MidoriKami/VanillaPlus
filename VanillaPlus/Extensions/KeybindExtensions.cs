using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.System.Input;

namespace VanillaPlus.Extensions;

public static class KeybindExtensions {
    public static bool IsKeybindMatch(this Keybind keybind, HashSet<VirtualKey> keys) {
        foreach (var keySetting in keybind.KeySettings) {
            if (keySetting.IsKeySettingMatch(keys)) return true;
        }

        return false;
    }

    public static bool IsKeySettingMatch(this KeySetting keySetting, HashSet<VirtualKey> keyCombo) {
        var key = keySetting.Key;
        var modifier = keySetting.KeyModifier;
        
        var comboKey = keyCombo.FirstOrDefault(keyComboKey => keyComboKey is not (VirtualKey.CONTROL  or VirtualKey.MENU or VirtualKey.SHIFT));
        var comboModifies = keyCombo.Where(keyComboKey => keyComboKey is (VirtualKey.CONTROL or VirtualKey.MENU or VirtualKey.SHIFT)).ToList();
        
        // If our base key doesn't match, we don't match.
        if ((int)comboKey != (int)key) return false;

        // Game combo does not have a modifier, but we do
        if (modifier is KeyModifierFlag.None && comboModifies.Count != 0) return false;
        
        // Game combo wants Control, but we don't have Control in our combo
        if (modifier.HasFlag(KeyModifierFlag.Ctrl) && !comboModifies.Contains(VirtualKey.CONTROL)) return false;
        
        // Game combo wants Alt, but we don't have Alt in our combo
        if (modifier.HasFlag(KeyModifierFlag.Alt) && !comboModifies.Contains(VirtualKey.MENU)) return false;
        
        // Game combo wants Shift, but we don't have Shift in our combo
        if (modifier.HasFlag(KeyModifierFlag.Shift) && !comboModifies.Contains(VirtualKey.SHIFT)) return false;

        return true;
    }
}
