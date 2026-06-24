using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;

namespace VanillaPlus.Features.ZoneTransitionLabels;

public class ZoneWatcher : IDisposable
{
    public List<ZoneExit> ZoneExits = [];

    public ZoneWatcher()
    {
        Services.ClientState.Login += OnLogin;
        Services.ClientState.ZoneInit += OnZoneInit;
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
    private void OnZoneInit(ZoneInitEventArgs obj) => UpdateExits();

    private void UpdateExits()
    {
        ZoneExits.Clear();

        Task.Run(async () => {
            uint loadState;

            unsafe {
                loadState = GameMain.Instance()->TerritoryLoadState;
            }

            while (loadState is not 2) {
                await Task.Delay(16);
                unsafe {
                    loadState = GameMain.Instance()->TerritoryLoadState;
                }
            }

            await BuildAssociations();
        });
    }

    /// <summary>
    /// Combines LineVfx instances and their closest ExitRange neighbor.
    /// </summary>
    private unsafe Task BuildAssociations()
    {
        var active = LayoutWorld.Instance()->ActiveLayout;

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
                ZoneExits.Add(new ZoneExit(line, closest));
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Services.ClientState.Login -= OnLogin;
        Services.ClientState.ZoneInit -= OnZoneInit;
    }
}
