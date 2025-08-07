using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using VanillaPlus.MiniCactpotHelper;

namespace VanillaPlus.Features.MiniCactpotHelper;

public class MiniCactpotHelperConfigWindow(MiniCactpotHelperConfig config, Action onConfigChanged) : Window("Mini Cactpot Helper Config", ImGuiWindowFlags.AlwaysAutoResize) {
    public override void Draw() {
        DrawAnimationConfig();
        DrawIconConfig();
        DrawColorConfig();
    }
    
    private void DrawAnimationConfig() {
        DrawHeader("Animations");
        
        if (ImGui.Checkbox("Enable Animations", ref config.EnableAnimations)) {
            onConfigChanged();
            config.Save();
        }
    }
	
    private void DrawIconConfig() {
        DrawHeader("Icon");
        
        if (GameIconButton(61332)) {
            config.IconId = 61332;
            onConfigChanged();
            config.Save();
        }

        ImGui.SameLine();
		
        if (GameIconButton(90452)) {
            config.IconId = 90452;
            onConfigChanged();
            config.Save();
        }
		
        ImGui.SameLine();
		
        if (GameIconButton(234008)) {
            config.IconId = 234008;
            onConfigChanged();
            config.Save();
        }
		
        ImGui.Spacing();
		
        ImGui.AlignTextToFramePadding();
        ImGui.Text("IconId:");
		
        ImGui.SameLine();
		
        var iconId = (int) config.IconId;
        if (ImGui.InputInt("##IconId", ref iconId)) {
            config.IconId = (uint) iconId;
            onConfigChanged();
            config.Save();
        }
    }
	
    private void DrawColorConfig() {
        DrawHeader("Colors");

        if (ImGui.ColorEdit4("Button Colors", ref config.ButtonColor, ImGuiColorEditFlags.AlphaPreviewHalf)) {
            onConfigChanged();
            config.Save();
        }

        if (ImGui.ColorEdit4("Lane Colors", ref config.LaneColor, ImGuiColorEditFlags.AlphaPreviewHalf)) {
            onConfigChanged();
            config.Save();
        }
    }

    private void DrawHeader(string text) {
        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.Text(text);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5.0f);
    }
    
    private bool GameIconButton(uint iconId) {
        var iconTexture = Services.TextureProvider.GetFromGameIcon(iconId);
        
        return ImGui.ImageButton(iconTexture.GetWrapOrEmpty().Handle, new Vector2(48.0f, 48.0f));
    }
        
    public override void OnClose()
        => config.Save();
}
