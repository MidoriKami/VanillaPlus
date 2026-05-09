using System;
using System.Numerics;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.DevFeatures.DebugImGuiWindow;

#if DEBUG
/// <summary>
/// Debug ImGui Window for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugWindow : Window, IDisposable {

    public DebugWindow() : base("Vanilla Plus Debug Window") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500.0f, 500.0f),
            MaximumSize = new Vector2(500.0f, 500.0f),
        };
    }

    public void Dispose() {

    }

    public override void Draw() {

    }
}
#endif
