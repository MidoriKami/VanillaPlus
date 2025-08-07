using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using VanillaPlus.BetterCursor;

namespace VanillaPlus.Features.BetterCursor;

public class BetterCursorConfigWindow(BetterCursorConfig config, Action onConfigChanged) : Window("Better Cursor Config", ImGuiWindowFlags.AlwaysAutoResize) {
    public override void Draw() {
        if (ImGui.ColorEdit4("Color", ref config.Color)) {
            onConfigChanged();
            config.Save();
        }

        if (ImGui.DragFloat("Size", ref config.Size)) {
            onConfigChanged();
            config.Save();
        }

        if (ImGui.Checkbox("Enable Animation", ref config.Animations)) {
            onConfigChanged();
            config.Save();
        }

        if (ImGui.Checkbox("Hide on Left-Hold or Right-Hold", ref config.HideOnCameraMove)) {
            config.Save();
        }
    }

    public override void OnClose()
        => config.Save();
}
