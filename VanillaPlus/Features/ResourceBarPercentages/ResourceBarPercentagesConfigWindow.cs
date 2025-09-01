using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.Features.ResourceBarPercentages;

public class ResourceBarPercentagesConfigWindow(ResourceBarPercentagesConfig config, Action onConfigChanged) : Window("Resource Bars as Percentages Config", ImGuiWindowFlags.AlwaysAutoResize) {
    public override void Draw() {
        if (ImGui.Checkbox("Show Percentages on Party List", ref config.PartyListEnabled)) {
            onConfigChanged();
            config.Save();
        }
        if (ImGui.Checkbox("Show Only Self on Party List", ref config.PartyListSelfOnly)) config.Save();

        ImGui.Spacing();
        ImGui.Spacing();

        if (ImGui.Checkbox("Show Percentages on Parameter Widget", ref config.ParameterWidgetEnabled)) {
            onConfigChanged();
            config.Save();
        }

        ImGui.Spacing();
        ImGui.Spacing();

        if (ImGui.Checkbox("Show Percentage Sign (%)", ref config.PercentageSignEnabled)) config.Save();
        if (ImGui.SliderInt("Decimal Places", ref config.DecimalPlaces, 0, 2)) config.Save();
    }

    public override void OnClose()
        => config.Save();
}
