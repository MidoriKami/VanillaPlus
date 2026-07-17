using System.Threading.Tasks;
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

    public override Task OnEnableAsync() {
        windowSystem = new WindowSystem("VanillaPlus - Debug");
        debugWindow = new DebugWindow {
            IsOpen = true,
        };

        windowSystem.AddWindow(debugWindow);

        OpenConfigAction = debugWindow.Toggle;

        VanillaPlus.PluginInterface.UiBuilder.Draw += DrawImGui;

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        VanillaPlus.PluginInterface.UiBuilder.Draw -= DrawImGui;

        windowSystem?.RemoveAllWindows();
        windowSystem = null;

        debugWindow?.Dispose();
        debugWindow = null;

        return Task.CompletedTask;
    }

    private void DrawImGui() {
        windowSystem?.Draw();


    }
}
#endif
