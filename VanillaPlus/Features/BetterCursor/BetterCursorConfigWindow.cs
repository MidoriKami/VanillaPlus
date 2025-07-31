﻿using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace VanillaPlus.BetterCursor;

public class BetterCursorConfigWindow(BetterCursorConfig config, Action onColorChanged, Action toggleAnimation, Action sizeChanged) : Window("Better Cursor Config", ImGuiWindowFlags.AlwaysAutoResize) {
    public override void Draw() {
        if (ImGui.ColorEdit4("Color", ref config.Color)) {
            onColorChanged();
            config.Save();
        }

        if (ImGui.DragFloat("Size", ref config.Size)) {
            sizeChanged();
            config.Save();
        }

        if (ImGui.Checkbox("Enable Animation", ref config.Animations)) {
            toggleAnimation();
            config.Save();
        }

        if (ImGui.Checkbox("Hide on Left-Hold or Right-Hold", ref config.HideOnCameraMove)) {
            config.Save();
        }
    }

    public override void OnClose()
        => config.Save();
}
