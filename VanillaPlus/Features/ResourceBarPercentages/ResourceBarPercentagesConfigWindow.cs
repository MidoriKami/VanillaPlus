using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.Features.ResourceBarPercentages;

public class ResourceBarPercentagesConfigWindow(ResourceBarPercentagesConfig config, Action onConfigChanged) : Window("Resource Bars as Percentages Config", ImGuiWindowFlags.AlwaysAutoResize) {
    public override void Draw() {
        if (ImGui.Checkbox("Show on Party List", ref config.PartyListEnabled)) SaveConfigWithCallback();

        if (config.PartyListEnabled) {
            using var indent = ImRaii.PushIndent();

            if (ImGui.Checkbox("Player", ref config.PartyListSelf)) SaveConfigWithCallback();
            if (ImGui.Checkbox("Party Members", ref config.PartyListMembers)) SaveConfigWithCallback();
            
            ImGui.Spacing();
            
            if (ImGui.Checkbox("HP##Party", ref config.PartyListHpEnabled)) SaveConfigWithCallback();
            if (ImGui.Checkbox("MP##Party", ref config.PartyListMpEnabled)) SaveConfigWithCallback();
            if (ImGui.Checkbox("GP##Party", ref config.PartyListGpEnabled)) SaveConfigWithCallback();
            
            if (ImGui.IsItemHovered()) {
                ImGui.BeginTooltip();
                ImGui.Text("GP is only shown on the player.");
                ImGui.EndTooltip();
            }
            
            if (ImGui.Checkbox("CP##Party", ref config.PartyListCpEnabled)) SaveConfigWithCallback();
            
            if (ImGui.IsItemHovered()) {
                ImGui.BeginTooltip();
                ImGui.Text("CP is only shown on the player.");
                ImGui.EndTooltip();
            }
        }

        ImGui.Spacing();

        if (ImGui.Checkbox("Show on Parameter Widget", ref config.ParameterWidgetEnabled)) SaveConfigWithCallback();

        if (config.ParameterWidgetEnabled) {
            using var indent = ImRaii.PushIndent();

            if (ImGui.Checkbox("HP##Parameter", ref config.ParameterHpEnabled)) SaveConfigWithCallback();
            if (ImGui.Checkbox("MP##Parameter", ref config.ParameterMpEnabled)) SaveConfigWithCallback();
            if (ImGui.Checkbox("GP##Parameter", ref config.ParameterGpEnabled)) SaveConfigWithCallback();
            if (ImGui.Checkbox("CP##Parameter", ref config.ParameterCpEnabled)) SaveConfigWithCallback();
        }

        ImGui.Spacing();

        if (ImGui.Checkbox("Show Percentage Sign (%)", ref config.PercentageSignEnabled)) SaveConfigWithCallback();

        ImGui.Spacing();

        if (ImGui.SliderInt("Decimal Places", ref config.DecimalPlaces, 0, 2)) SaveConfigWithCallback();
        if (ImGui.Checkbox("Show decimals only below 100%", ref config.ShowDecimalsBelowHundredOnly)) SaveConfigWithCallback();
    }

    private void SaveConfigWithCallback() {
        config.Save();
        onConfigChanged();
    }

    public override void OnClose()
        => SaveConfigWithCallback();
}
