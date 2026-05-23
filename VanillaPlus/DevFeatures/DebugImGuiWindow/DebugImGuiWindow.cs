using Dalamud.Interface.Windowing;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.DevFeatures.DebugImGuiWindow;

#if DEBUG
/// <summary>
/// Debug Game Modification with a Custom ImGui Window for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugImGuiWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Debug ImGui Window",
        Description = "Displays a custom ImGui window to debug/dev purposes.",
        Type = ModificationType.Debug,
        Authors = ["YourNameHere"],
    };

    private WindowSystem? windowSystem;
    private DebugWindow? debugWindow;

    public override void OnEnableAsync() {
        windowSystem = new WindowSystem("VanillaPlus - Debug");
        debugWindow = new DebugWindow {
            IsOpen = true,
        };

        windowSystem.AddWindow(debugWindow);

        OpenConfigAction = debugWindow.Toggle;

        Services.PluginInterface.UiBuilder.Draw += DrawImGui;
    }

    public override void OnDisableAsync() {
        Services.PluginInterface.UiBuilder.Draw -= DrawImGui;

        windowSystem?.RemoveAllWindows();
        windowSystem = null;

        debugWindow?.Dispose();
        debugWindow = null;
    }

    private void DrawImGui() {
        windowSystem?.Draw();


    }
}
#endif
