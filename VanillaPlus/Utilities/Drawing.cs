using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace VanillaPlus.Utilities;

public static class Drawing {
    public static bool IconButton(FontAwesomeIcon icon, string id = "") {
        using var pushedId = ImRaii.PushId(id);
        using var fixedFont = Services.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push();
        return ImGui.Button(icon.ToIconString(), ImGuiHelpers.ScaledVector2(25.0f, 25.0f));
    }

    public static bool KeybindConfig(HashSet<VirtualKey> keys, ref VirtualKey newKey) {
        var result = false;
        
        foreach (var keybind in keys.ToList()) {
            if (IconButton(FontAwesomeIcon.Trash, keybind.ToString())) {
                keys.Remove(keybind);
                result = true;
            }
            
            ImGui.SameLine();
            
            ImGui.Text(keybind.ToString());
        }

        if (IconButton(FontAwesomeIcon.Plus, "newKey")) {
            keys.Add(newKey);
            result = true;
        }
        
        ImGui.SameLine();

        using var dropDown = ImRaii.Combo("##newKeyCombo", newKey.ToString(), ImGuiComboFlags.HeightLarge | ImGuiComboFlags.PopupAlignLeft);
        if (dropDown) {
            foreach (var key in Enum.GetValues<VirtualKey>()) {
                if (ImGui.Selectable(key.ToString(), key == newKey)) {
                    newKey = key;
                    result = true;
                }
            }
        }
        
        return result;
    }
}
