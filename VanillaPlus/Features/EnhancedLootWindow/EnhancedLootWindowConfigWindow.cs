using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.Features.EnhancedLootWindow;

public class EnhancedLootWindowConfigWindow(EnhancedLootWindowConfig config) : Window("Enhanced Loot Window Config", ImGuiWindowFlags.AlwaysAutoResize) {
    public override void Draw() {
        if (ImGui.Checkbox("Mark Un-obtainable Items", ref config.MarkUnobtainableItems)) config.Save();
        if (ImGui.Checkbox("Mark Already Unlocked Items", ref config.MarkAlreadyObtainedItems)) config.Save();
    }

    public override void OnClose()
        => config.Save();
}
