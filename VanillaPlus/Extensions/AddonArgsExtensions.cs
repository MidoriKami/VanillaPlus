using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AddonArgsExtensions {
    public static T* GetAddon<T>(this AddonArgs args) where T : unmanaged
        => (T*)args.Addon.Address;

    public static void PrintAtkValues(this AddonArgs args) {
        var atkValues = args switch {
            AddonRefreshArgs refreshArgs => new Span<AtkValue>((AtkValue*)refreshArgs.AtkValues, (int)refreshArgs.AtkValueCount),
            AddonSetupArgs setupArgs => new Span<AtkValue>((AtkValue*)setupArgs.AtkValues, (int)setupArgs.AtkValueCount),
            _ => throw new Exception("Invalid Args Type"),
        };

        foreach (var index in Enumerable.Range(0, atkValues.Length)) {
            ref var value = ref atkValues[index];
            if (value.Type is 0) continue;
            
            Services.PluginLog.Debug($"[{index,4}]{value.GetValueAsString()}");
        }
    }

    public static Span<AtkValue> GetAtkValues(this AddonArgs args)
        => args.GetAddon<AtkUnitBase>()->AtkValuesSpan;

    private static AtkEventData.AtkMouseData* GetMouseData(this AddonArgs args) {
        if (args is not AddonReceiveEventArgs eventArgs) return null;

        return &((AtkEventData*)eventArgs.Data)->MouseData;
    }

    public static Vector2 GetMouseClickPosition(this AddonArgs args) {
        var mouseData = GetMouseData(args);
        if (mouseData is null) return Vector2.Zero;

        return new Vector2(mouseData->PosX, mouseData->PosY);
    }
}
