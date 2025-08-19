using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using VanillaPlus.Extensions;

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
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Checkbox("Enable Animation", ref config.Animations)) {
            onConfigChanged();
            config.Save();
        }

        if (ImGui.Checkbox("Hide on Left-Hold or Right-Hold", ref config.HideOnCameraMove)) {
            config.Save();
        }
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Checkbox("Only Show in Combat", ref config.OnlyShowInCombat)) {
            config.Save();
        }

        if (ImGui.Checkbox("Only Show in Duties", ref config.OnlyShowInDuties)) {
            config.Save();
        }
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.InputUInt("IconId", ref config.IconId, 1)) {
            onConfigChanged();
            config.Save();
        }

        if (Services.TextureProvider.TryGetFromGameIcon(config.IconId, out var texture)) {
            ImGui.Image(texture.GetWrapOrEmpty().Handle, ImGuiHelpers.ScaledVector2(128.0f, 128.0f));
        }
        else {
            ImGui.Image(Services.TextureProvider.GetPlaceholderTexture().GetWrapOrEmpty().Handle, ImGuiHelpers.ScaledVector2(128.0f, 128.0f));
        }
    }

    public override void OnClose()
        => config.Save();
}
