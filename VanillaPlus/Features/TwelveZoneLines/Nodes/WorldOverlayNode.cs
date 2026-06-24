using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using KamiToolKit.Enums;
using KamiToolKit.UiOverlay;
using Matrix4x4 = FFXIVClientStructs.FFXIV.Common.Math.Matrix4x4;

namespace VanillaPlus.Features.TwelveZoneLines.Nodes;

public abstract unsafe class WorldOverlayNode : OverlayNode
{
    public new abstract Vector3 Position { get; set; }

    public override OverlayLayer OverlayLayer => OverlayLayer.Background;

    protected override void OnUpdate()
    {
        var framework = Framework.Instance();
        OnUpdate(framework->FrameDeltaTime);

        IsVisible = WorldToScreen(Position, out var screenPos) && IsVisible;
        base.Position = screenPos - base.Origin;
    }

    protected abstract void OnUpdate(float deltaTime);

    // ---- Literally a copy of Dalamud's GameGui.WorldToScreen but fixed to be a frame ahead and not jittery with DLSS ----
    // (I couldn't get it into Dalamud on time considering the frame issue is bad for ImGui :c )

    private static bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos)
        => WorldToScreen(worldPos, out screenPos, out var inView) && inView;

    private static bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos, out bool inView)
    {
        var windowPos = ImGuiHelpers.MainViewport.Pos;

        var cameraMan = CameraManager.Instance();
        var activeCamera = cameraMan->Cameras[cameraMan->ActiveCameraIndex].Value;
        var renderCamera = activeCamera->SceneCamera.RenderCamera;

        var view = renderCamera->ViewMatrix;
        var proj = renderCamera->ProjectionMatrix;
        var viewProjectionMatrix = view * proj;

        var device = Device.Instance();
        float width = device->Width;
        float height = device->Height;

        var pCoords = Vector4.Transform(new Vector4(worldPos, 1.0f), viewProjectionMatrix);
        var inFront = pCoords.W > 0.0f;

        if (Math.Abs(pCoords.W) < float.Epsilon)
        {
            screenPos = Vector2.Zero;
            inView = false;
            return false;
        }

        pCoords *= MathF.Abs(1.0f / pCoords.W);
        screenPos = new Vector2(pCoords.X, pCoords.Y);

        screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPos.X;
        screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPos.Y;

        inView = inFront &&
                 screenPos.X > windowPos.X && screenPos.X < windowPos.X + width &&
                 screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + height;

        return inFront;
    }
}
