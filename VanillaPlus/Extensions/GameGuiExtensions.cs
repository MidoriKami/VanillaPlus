using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

namespace VanillaPlus.Extensions;

/// <summary>
/// Extensions for Dalamud's IGameGui service.
/// </summary>
public static unsafe class GameGuiExtensions {
    extension(IGameGui gui) {
        /// <summary>
        /// Same as IGameGui's WorldToScreen, but adjusted to be a frame ahead, and fixes jitter issues with DLSS.
        /// </summary>
        /// <inheritdoc cref="IGameGui.WorldToScreen(Vector3, out Vector2)"/>
        /// <seealso cref="IGameGui.WorldToScreen(Vector3, out Vector2)"/>
        public bool WorldToScreenAdjusted(Vector3 worldPos, out Vector2 screenPos)
            => gui.WorldToScreenAdjusted(worldPos, out screenPos, out var inView) && inView;

        /// <summary>
        /// Same as IGameGui's WorldToScreen, but adjusted to be a frame ahead, and fixes jitter issues with DLSS.
        /// </summary>
        /// <inheritdoc cref="IGameGui.WorldToScreen(Vector3, out Vector2, out bool)"/>
        /// <seealso cref="IGameGui.WorldToScreen(Vector3, out Vector2, out bool)"/>
        public bool WorldToScreenAdjusted(Vector3 worldPos, out Vector2 screenPos, out bool inView)
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
}
