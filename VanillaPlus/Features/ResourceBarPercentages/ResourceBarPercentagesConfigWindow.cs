using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.Features.ResourceBarPercentages;

public class ResourceBarPercentagesConfigWindow(ResourceBarPercentagesConfig config, Action onConfigChanged) : Window("Resource Bars as Percentages Config", ImGuiWindowFlags.AlwaysAutoResize) {
    public override void Draw() {
        if (ImGui.Checkbox("Show on Party List", ref config.PartyListEnabled)) SaveConfigWithCallback();

        if (config.PartyListEnabled) {
            ImGui.Text("  ");
            ImGui.SameLine();
            if (ImGui.Checkbox("Player", ref config.PartyListSelf)) SaveConfigWithCallback();

            ImGui.Text("  ");
            ImGui.SameLine();
            if (ImGui.Checkbox("Other Party Members", ref config.PartyListOtherMembers)) SaveConfigWithCallback();
        }

        ImGui.NewLine();

        if (ImGui.Checkbox("Show on Parameter Widget", ref config.ParameterWidgetEnabled)) SaveConfigWithCallback();

        if (config.ParameterWidgetEnabled) {
            ImGui.Text("  ");
            ImGui.SameLine();
            if (ImGui.Checkbox("HP", ref config.ParameterHpEnabled)) SaveConfigWithCallback();

            ImGui.Text("  ");
            ImGui.SameLine();
            if (ImGui.Checkbox("MP", ref config.ParameterMpEnabled)) SaveConfigWithCallback();

            ImGui.Text("  ");
            ImGui.SameLine();
            if (ImGui.Checkbox("GP", ref config.ParameterGpEnabled)) SaveConfigWithCallback();

            ImGui.Text("  ");
            ImGui.SameLine();
            if (ImGui.Checkbox("CP", ref config.ParameterCpEnabled)) SaveConfigWithCallback();
        }

        ImGui.NewLine();

        if (ImGui.Checkbox("Show Percentage Sign (%)", ref config.PercentageSignEnabled)) SaveConfigWithCallback();
        if (ImGui.SliderInt("Decimal Places", ref config.DecimalPlaces, 0, 2)) SaveConfigWithCallback();
    }

    private void SaveConfigWithCallback() {
        config.Save();
        onConfigChanged();
    }

    public override void OnClose()
        => SaveConfigWithCallback();
}
