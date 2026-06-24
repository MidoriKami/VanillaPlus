using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;

namespace VanillaPlus.Features.TwelveZoneLines;

public unsafe class ZoneWatcher : IDisposable
{
    public List<ZoneExit> ZoneExits = [];
    public ZoneExit? ClosestExit;

    public delegate void TerritoryLoadedDelegate();

    // ty winter
    [Signature("48 8b 05 ?? ?? ?? 02 c6 40 0a 01", DetourName = nameof(TerritoryLoaded))]
    private readonly Hook<TerritoryLoadedDelegate>? territoryLoadedHook = null; // Technically related to CullingManager doesnt matter

    public ZoneWatcher()
    {
        Services.ClientState.Login += OnLogin;
        UpdateExits();

        Services.GameInteropProvider.InitializeFromAttributes(this);
        territoryLoadedHook?.Enable();
    }

    public void TerritoryLoaded()
    {
        territoryLoadedHook!.Original.Invoke();
        UpdateExits();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneExit GetClosestExit(Vector3 target, out Vector3 closestPoint)
    {
        ZoneExit exit = default;
        closestPoint = Vector3.Zero;

        var closestDistance = float.MaxValue;
        foreach (var zoneExit in ZoneExits)
        {
            var point = zoneExit.GetClosestPoint(target);
            var distSq = Vector3.DistanceSquared(target, point);

            if (distSq < closestDistance)
            {
                exit = zoneExit;
                closestPoint = point;
                closestDistance = distSq;
            }
        }

        return exit;
    }

    private void OnLogin() => UpdateExits();

    private void UpdateExits()
    {
        ZoneExits.Clear();
        if (TryBuildAssociations(out var exits))
        {
            ZoneExits = exits;
        }
    }

    /// <summary>
    /// Combines LineVfx instances and their closest ExitRange neighbor.
    /// </summary>
    /// <param name="exits">Associated instances</param>
    /// <returns>True if successful</returns>
    public static bool TryBuildAssociations(out List<ZoneExit> exits)
    {
        exits = [];
        var world = LayoutWorld.Instance();
        if (world == null) return false;

        var active = world->ActiveLayout;
        if (active == null) return false;

        List<IntPtr> exitRangeInstances = [];
        List<IntPtr> lineVfxInstances = [];

        foreach (var (_, layer) in active->Layers)
        {
            if (layer.IsNull) continue;

            foreach (var (_, instance) in layer.Value->Instances)
            {
                if (instance.IsNull) continue;

                switch (instance.Value->Id.Type)
                {
                    case InstanceType.ExitRange:
                        exitRangeInstances.Add((IntPtr)instance.Value);
                        continue;
                    case InstanceType.LineVfx:
                        lineVfxInstances.Add((IntPtr)instance.Value);
                        continue;
                    default:
                        continue;
                }
            }
        }

        foreach (var ptr in lineVfxInstances)
        {
            var line = (LineVfxLayoutInstance*)ptr;

            var line2D = new Vector2(line->Transform.Translation.X, line->Transform.Translation.Y);

            var smallestDistance = float.MaxValue;
            ExitRangeLayoutInstance* closest = null;
            foreach (var rangePtr in exitRangeInstances)
            {
                var range = (ExitRangeLayoutInstance*)rangePtr;
                if (range->ExitType != ExitRangeType.ZoneLine) continue;

                var transform = range->Transform;

                var range2D = new Vector2(transform.Translation.X, transform.Translation.Y);
                var dist = Vector2.DistanceSquared(line2D, range2D);

                if (dist < smallestDistance)
                {
                    smallestDistance = dist;
                    closest = range;
                }
            }

            if (closest != null)
            {
                exits.Add(new ZoneExit(line, closest));
            }
        }

        return true;
    }

    public void Dispose()
    {
        Services.ClientState.Login -= OnLogin;

        territoryLoadedHook?.Dispose();
    }
}
