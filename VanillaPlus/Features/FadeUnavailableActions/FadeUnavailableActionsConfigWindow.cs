using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.Features.FadeUnavailableActions;

public class FadeUnavailableActionsConfigWindow(FadeUnavailableActionsConfig config) : Window("Fade Unavailable Actions Config", ImGuiWindowFlags.AlwaysAutoResize) {
    public override void Draw() {
        if (ImGui.SliderInt("Fade Percentage", ref config.FadePercentage, 0, 90)) config.Save();
        if (ImGui.SliderInt("Redden Percentage", ref config.ReddenPercentage, 5, 100)) config.Save();

        ImGui.Spacing();

        if (ImGui.Checkbox("Apply Transparency to Frame", ref config.ApplyToFrame)) config.Save();
        if (ImGui.Checkbox("Apply only to Sync'd Actions", ref config.ApplyToSyncActions)) config.Save();
        if (ImGui.Checkbox("Redden skills out of range", ref config.ReddenOutOfRange)) config.Save();
    }

    public override void OnClose()
        => config.Save();
}
