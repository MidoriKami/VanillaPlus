using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.Features.ClearSelectedDuties;

public class ClearSelectedDutiesConfigWindow(ClearSelectedDutiesConfig config) : Window("Clear Selected Duties Config", ImGuiWindowFlags.AlwaysAutoResize) {
    public override void Draw() {
        if (ImGui.Checkbox("Disable While Unrestricted", ref config.DisableWhenUnrestricted)) {
            config.Save();
        }
        
        ImGuiComponents.HelpMarker("When Unrestricted Party is enabled,\n" +
                                   "the duty finder will not have its duties unselected");
    }

    public override void OnClose()
        => config.Save();
}
