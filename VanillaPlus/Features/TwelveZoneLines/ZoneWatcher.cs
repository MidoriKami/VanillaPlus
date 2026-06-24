using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

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
        if (LayoutMatcher.TryBuildAssociations(out var exits))
        {
            ZoneExits = exits;
        }
    }

    public void Dispose()
    {
        Services.ClientState.Login -= OnLogin;

        territoryLoadedHook?.Dispose();
    }
}
