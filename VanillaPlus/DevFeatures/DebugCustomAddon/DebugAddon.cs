using System;
using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace VanillaPlus.DevFeatures.DebugCustomAddon;

#if DEBUG
/// <summary>
/// Debug Addon Window for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugAddon : NativeAddon {

    private ImGuiImageNode? customImageNode;
    private IDrawListTextureWrap? textureWrap;

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        SetWindowSize(new Vector2(750.0f, 400.0f));

        customImageNode = new ImGuiImageNode {
            Size = ContentSize,
            Position = ContentStartPosition,
        };
        customImageNode.AttachNode(this);

        Services.PluginInterface.UiBuilder.Draw += InitializeDraw;
    }

    public override void Dispose() {
        Services.PluginInterface.UiBuilder.Draw -= InitializeDraw;

        base.Dispose();
    }

    private void InitializeDraw() {
        if (textureWrap is null) {
            using var drawData = BufferBackedImDrawData.Create();

            var size = new Vector2(64.0f, 64.0f);

            // drawData.ListPtr.AddCircleFilled(size / 2.0f, 32.0f, ImGui.GetColorU32(KnownColor.ForestGreen.Vector()));

            drawData.DataPtr.DisplaySize = size;
            drawData.UpdateDrawDataStatistics();

            textureWrap = Services.TextureProvider.CreateDrawListTexture("VanillaPlusTest");

            textureWrap.Size = size;

            // textureWrap.Draw(drawData.DataPtr);
            textureWrap.ResizeAndDrawWindow("", Vector2.One);
            customImageNode?.LoadTexture(textureWrap);
        }

        textureWrap.ResizeAndDrawWindow("", Vector2.One);
        customImageNode?.TextureSize = textureWrap.Size;
    }
}
#endif
