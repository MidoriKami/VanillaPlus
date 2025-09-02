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
            if (ImGui.Checkbox("Other Party Members", ref config.PartyListOtherMembers)) SaveConfigWithCallback();
        }

        ImGui.Spacing();

        if (ImGui.Checkbox("Show on Parameter Widget", ref config.ParameterWidgetEnabled)) SaveConfigWithCallback();

        if (config.ParameterWidgetEnabled) {
            using var indent = ImRaii.PushIndent();

            if (ImGui.Checkbox("HP", ref config.ParameterHpEnabled)) SaveConfigWithCallback();
            if (ImGui.Checkbox("MP", ref config.ParameterMpEnabled)) SaveConfigWithCallback();
            if (ImGui.Checkbox("GP", ref config.ParameterGpEnabled)) SaveConfigWithCallback();
            if (ImGui.Checkbox("CP", ref config.ParameterCpEnabled)) SaveConfigWithCallback();
        }

        ImGui.Spacing();

        if (ImGui.Checkbox("Show Percentage Sign (%)", ref config.PercentageSignEnabled)) SaveConfigWithCallback();
        
        ImGui.Spacing();
        
        if (ImGui.SliderInt("Decimal Places", ref config.DecimalPlaces, 0, 2)) SaveConfigWithCallback();
    }

    private void SaveConfigWithCallback() {
        config.Save();
        onConfigChanged();
    }

    public override void OnClose()
        => SaveConfigWithCallback();
}
