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
        public static bool WorldToScreenAdjusted(Vector3 worldPos, out Vector2 screenPos)
            => IGameGui.WorldToScreenAdjusted(worldPos, out screenPos, out var inView) && inView;

        /// <summary>
        /// Same as IGameGui's WorldToScreen, but adjusted to be a frame ahead, and fixes jitter issues with DLSS.
        /// </summary>
        /// <inheritdoc cref="IGameGui.WorldToScreen(Vector3, out Vector2, out bool)"/>
        /// <seealso cref="IGameGui.WorldToScreen(Vector3, out Vector2, out bool)"/>
        public static bool WorldToScreenAdjusted(Vector3 worldPos, out Vector2 screenPos, out bool inView) {
            var windowPosition = ImGuiHelpers.MainViewport.Pos;

            var cameraManager = CameraManager.Instance();
            var activeCamera = cameraManager->Cameras[cameraManager->ActiveCameraIndex].Value;
            var renderCamera = activeCamera->SceneCamera.RenderCamera;

            var view = renderCamera->ViewMatrix;
            var projection = renderCamera->ProjectionMatrix;
            var viewProjectionMatrix = view * projection;

            var device = Device.Instance();
            float width = device->Width;
            float height = device->Height;

            var projectionCoords = Vector4.Transform(new Vector4(worldPos, 1.0f), viewProjectionMatrix);
            var inFront = projectionCoords.W > 0.0f;

            if (Math.Abs(projectionCoords.W) < float.Epsilon) {
                screenPos = Vector2.Zero;
                inView = false;
                return false;
            }

            projectionCoords *= MathF.Abs(1.0f / projectionCoords.W);
            screenPos = new Vector2(projectionCoords.X, projectionCoords.Y);

            screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPosition.X;
            screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPosition.Y;

            inView = inFront &&
                     screenPos.X > windowPosition.X && screenPos.X < windowPosition.X + width &&
                     screenPos.Y > windowPosition.Y && screenPos.Y < windowPosition.Y + height;

            return inFront;
        }
    }
}
