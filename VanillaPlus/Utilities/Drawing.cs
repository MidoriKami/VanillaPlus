using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace VanillaPlus.Utilities;

public static class Drawing {
    public static bool IconButton(FontAwesomeIcon icon, string id = "") {
        using var pushedId = ImRaii.PushId(id);
        using var fixedFont = Services.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push();
        return ImGui.Button(icon.ToIconString(), ImGuiHelpers.ScaledVector2(25.0f, 25.0f));
    }

    public static bool KeybindConfig(HashSet<SeVirtualKey> keys, ref SeVirtualKey newKey) {
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
            foreach (var key in Enum.GetValues<SeVirtualKey>()) {
                if (ImGui.Selectable(key.ToString(), key == newKey)) {
                    newKey = key;
                    result = true;
                }
            }
        }
        
        return result;
    }
}
