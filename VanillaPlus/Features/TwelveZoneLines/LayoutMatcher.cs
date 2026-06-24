using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Layer;

namespace VanillaPlus.Features.TwelveZoneLines;

public static unsafe class LayoutMatcher
{
    /// <summary>
    /// Combines LineVfx instances and their closest ZoneExit neighbor.
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
}
