using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.WindowBackground;

public unsafe class WindowBackgroundConfigWindow : Window {
    private readonly WindowBackgroundConfig config;
    private readonly Action configChangedCallback;
    private readonly Action styleChanged;
    private string searchString = string.Empty;

    public WindowBackgroundConfigWindow(WindowBackgroundConfig config, Action configChangedCallback, Action styleChanged) : base("Window Background Config") {
        this.config = config;
        this.configChangedCallback = configChangedCallback;
        this.styleChanged = styleChanged;

        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(350.0f, 500.0f),
        };
    }

    public override void Draw() {
        ImGui.Text("Color");
        
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.ColorEdit4("##Color", ref config.Color, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar)) styleChanged();
        if (ImGui.IsItemDeactivatedAfterEdit()) config.Save();

        ImGui.Spacing();
        ImGui.Text("Padding");
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.DragFloat2("##Padding", ref config.Padding, 0.1f, -50.0f, 50.0f)) styleChanged();
        if (ImGui.IsItemDeactivatedAfterEdit()) config.Save();
        
        ImGui.Spacing();
        ImGui.Separator();

        DrawEnabledAddons();
        DrawAllAddons();
    }

    private void DrawEnabledAddons() {
        ImGui.Text("Enabled Windows");
        using var child = ImRaii.Child("Enabled_Addons", new Vector2(ImGui.GetContentRegionAvail().X, 100.0f * ImGuiHelpers.GlobalScale));
        if (!child) return;
        ImGui.Spacing();
        
        ImGuiClip.ClippedDraw(config.Addons, DrawAddonOption, 25.0f * ImGuiHelpers.GlobalScale);
    }
    
    private void DrawAllAddons() {
        ImGui.Text("All Windows");
        using var child = ImRaii.Child("All_Addons_Select", ImGui.GetContentRegionAvail());
        if (!child) return;
        ImGui.Spacing();

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputTextWithHint("##Search", "Search...", ref searchString);
        ImGui.Spacing();
        
        var addonList = GetAddonNames().Where(name => name.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
        ImGuiClip.ClippedDraw(addonList, DrawAddonOption, 25.0f * ImGuiHelpers.GlobalScale);
    }

    private static List<string> GetAddonNames() {
        List<string> addonNames = [];

        foreach (var addon in RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries) {
            if (addon.Value is not null) {
                addonNames.Add(addon.Value->NameString);
            }
        }

        return addonNames;
    }

    private void DrawAddonOption(string addonName) {
        using var id = ImRaii.PushId(addonName);
        var isTracked = config.Addons.Contains(addonName);
        
        if (isTracked) {
            if (Drawing.IconButton(FontAwesomeIcon.Trash)) {
                config.Addons.Remove(addonName);
                configChangedCallback();
                config.Save();
            }
        }
        else {
            if (Drawing.IconButton(FontAwesomeIcon.Plus)) {
                config.Addons.Add(addonName);
                configChangedCallback();
                config.Save();
            }
        }
        
        ImGui.SameLine();
        ImGui.TextColored(isTracked ? KnownColor.LimeGreen.Vector() : KnownColor.White.Vector(), addonName);
    }
}
