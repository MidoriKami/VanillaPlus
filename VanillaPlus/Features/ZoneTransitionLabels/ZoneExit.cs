using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.Interop;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.ZoneTransitionLabels;

/// <summary>
/// Combines ExitRanges and LineVfx into one
/// </summary>
public struct ZoneExit {

    public RowRef<TerritoryType> TerritoryType;
    public Transform Transform;

    public bool IsValid => TerritoryType.IsValid;
    public string Name => IsValid ? TerritoryType.Value.PlaceName.Value.Name.ExtractText() : string.Empty;

    private readonly Vector3 up;
    private readonly Vector3 right;

    public unsafe ZoneExit(Pointer<LineVfxLayoutInstance> line, Pointer<ExitRangeLayoutInstance> exit) {
        TerritoryType = new RowRef<TerritoryType>(Services.DataManager.Excel, exit.Value->TerritoryType);
        Transform = line.Value->Transform;

        up = Vector3.Transform(Vector3.UnitY, Transform.Rotation);
        right = Vector3.Transform(Vector3.UnitX, Transform.Rotation);
    }

    /// <summary>
    /// Gets the closest point on the LineVfx object.
    /// </summary>
    /// <param name="target">Target point to search from</param>
    /// <returns>Closest point on the plane</returns>
    public Vector3 GetClosestPoint(Vector3 target) {
        var pos = Transform.Translation;
        var scale = Transform.Scale;

        var dir = target - pos;

        var localX = Vector3.Dot(dir, right);
        var localY = Vector3.Dot(dir, up);

        var clampedX = Math.Clamp(localX, -scale.X, scale.X);
        var clampedY = Math.Clamp(localY, -scale.Y, scale.Y);

        return pos + right * clampedX + up * clampedY;
    }

    /// <summary>
    /// Finds the closest point on the LineVfx object then casts downwards to adjust towards the ground.
    /// </summary>
    /// <param name="target">Target point to search from</param>
    /// <returns>Closest point on the plane, adjusted towards the ground.</returns>
    public Vector3 GetClosestGroundPoint(Vector3 target) {
        var closest = GetClosestPoint(target);
        var point = closest with { Y = Transform.Translation.Y + Transform.Scale.Y };

        if (BGCollisionModule.RaycastMaterialFilter(point, -Vector3.UnitY, out var hit)) {
            var bottom = Transform.Translation.Y - Transform.Scale.Y;
            point.Y = Math.Clamp(hit.Point.Y, bottom, point.Y);
        }
        else {
            point.Y = closest.Y;
        }

        return point;
    }
}
