using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DebugGameModification;

#if DEBUG
/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Debug GameModification",
        Description = "A module for playing around and testing VanillaPlus features",
        Type = ModificationType.Debug,
        Authors = [ "YourNameHere" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private class DemoWindow() : Window("Exception Test Window", ImGuiWindowFlags.AlwaysAutoResize) {
        public override void Draw() {
            ImGui.Text("butts");
            ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);
            throw new Exception("Boom");
            ImGui.Text("Butts2");
        }
    }
    
    private WindowSystem windowSystem = new WindowSystem("Debug Game Modification");
    private DemoWindow window = new DemoWindow();

    public override void OnEnable() {
        windowSystem.AddWindow(window);
        window.Open();
        
        Services.PluginInterface.UiBuilder.Draw += windowSystem.Draw;
    }

    public override void OnDisable() {
        Services.PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
    }
}
#endif
