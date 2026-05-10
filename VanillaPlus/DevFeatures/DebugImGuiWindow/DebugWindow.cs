using System;
using System.Drawing;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace VanillaPlus.DevFeatures.DebugImGuiWindow;

#if DEBUG
/// <summary>
/// Debug ImGui Window for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugWindow : Window, IDisposable {

    private IDrawListTextureWrap? textureWrap;

    public DebugWindow() : base("Vanilla Plus Debug Window") {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(500.0f, 500.0f),
            MaximumSize = new Vector2(500.0f, 500.0f),
        };
    }

    public void Dispose() {
        textureWrap?.Dispose();
    }

    public override void Draw() {
        if (textureWrap is null) {
            using var drawData = BufferBackedImDrawData.Create();

            var viewport = ImGui.GetMainViewport();

            drawData.ListPtr.AddRectFilled(Vector2.Zero, viewport.Size, ImGui.GetColorU32(KnownColor.ForestGreen.Vector()));

            drawData.DataPtr.DisplaySize = viewport.Size;
            drawData.UpdateDrawDataStatistics();

            textureWrap = Services.TextureProvider.CreateDrawListTexture("VanillaPlusTest");

            textureWrap.Size = viewport.Size;

            textureWrap.Draw(drawData.DataPtr);
        }

        ImGui.Image(textureWrap.Handle, textureWrap.Size);
    }
}
#endif
