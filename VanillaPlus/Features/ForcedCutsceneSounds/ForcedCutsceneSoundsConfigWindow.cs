using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.Features.ForcedCutsceneSounds;

public class ForcedCutsceneSoundsConfigWindow(ForcedCutsceneSoundsConfig config) : Window("Forced Cutscene Sounds Config", ImGuiWindowFlags.AlwaysAutoResize) {
    public override void Draw() {
        if (ImGui.Checkbox("Restore mute state after cutscene", ref config.Restore)) config.Save();
        ImGui.Separator();
        if (ImGui.Checkbox("Unmute Master Volume", ref config.HandleMaster)) config.Save();
        if (ImGui.Checkbox("Unmute BGM", ref config.HandleBgm)) config.Save();
        if (ImGui.Checkbox("Unmute Sound Effects", ref config.HandleSe)) config.Save();
        if (ImGui.Checkbox("Unmute Voice", ref config.HandleVoice)) config.Save();
        if (ImGui.Checkbox("Unmute Ambient Sounds", ref config.HandleEnv)) config.Save();
        if (ImGui.Checkbox("Unmute System Sounds", ref config.HandleSystem)) config.Save();
        if (ImGui.Checkbox("Unmute Performance", ref config.HandlePerform)) config.Save();
    }
    
    public override void OnClose()
        => config.Save();
}
