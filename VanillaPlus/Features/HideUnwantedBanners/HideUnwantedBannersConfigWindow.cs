using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.Features.HideUnwantedBanners;

public class HideUnwantedBannersConfigWindow : Window {
    private readonly List<int> banners = [
        120031, 120032, 120055, 120081, 120082, 120083, 120084,
        120085, 120086, 120093, 120094, 120095, 120096, 120141,
        120142, 121081, 121082, 121561, 121562, 121563, 128370,
        128371, 128372, 128373,
    ];

    private readonly HideUnwantedBannersConfig config;

    public HideUnwantedBannersConfigWindow(HideUnwantedBannersConfig config) : base("Hide Unwanted Banners Config") {
        this.config = config;
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(600.0f, 700.0f),
        };
    }
    
    public override void Draw() {
        DrawContentArea();
        DrawFooter();
    }

    private void DrawContentArea() {
        var footerOffset = new Vector2(0.0f, 30.0f * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().FramePadding.Y * 2.0f);
        
        using var child = ImRaii.Child("content_area", ImGui.GetContentRegionAvail() - footerOffset);
        if (!child) return;
        
        ImGuiClip.ClippedDraw(banners, DrawBannerSetting, 150.0f * ImGuiHelpers.GlobalScale);
    }
    
    private void DrawFooter() {
        using var child = ImRaii.Child("footer", ImGui.GetContentRegionAvail());
        if (!child) return;

        ImGui.Separator();
        
        if (ImGui.Button("Clear All", ImGuiHelpers.ScaledVector2(100.0f, 30.0f))) {
            config.HiddenBanners.Clear();
        }
        
        ImGui.SameLine(ImGui.GetContentRegionMax().X - 100.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.Button("Select All", ImGuiHelpers.ScaledVector2(100.0f, 30.0f))) {
            foreach (var banner in banners) {
                config.HiddenBanners.Add(banner);
            }
        }
    }

    private void DrawBannerSetting(int banner) {
        using var child = ImRaii.Child($"banner_{banner}", new Vector2(ImGui.GetContentRegionAvail().X, 150.0f * ImGuiHelpers.GlobalScale));
        if (!child) return;
        
        var isEnabled = config.HiddenBanners.Contains(banner);
        var texture = Services.TextureProvider.GetFromGameIcon(banner).GetWrapOrEmpty();
        var ratio = texture.Size.Y / ImGui.GetContentRegionMax().Y;

        if (ImGui.Selectable($"##banner_{banner}_selectable", isEnabled, ImGuiSelectableFlags.AllowItemOverlap, ImGui.GetContentRegionMax())) {
            if (isEnabled) {
                config.HiddenBanners.Remove(banner);
            }
            else {
                config.HiddenBanners.Add(banner);
            }
            
            config.Save();
        }
        
        ImGui.SetCursorPos(Vector2.Zero);
        ImGui.Image(texture.Handle, texture.Size / ratio);
    }
    
    public override void OnClose()
        => config.Save();
}
