using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AddonArgsExtensions {
    extension(AddonArgs args) {
        public Span<AtkValue> ValueSpan => args.GetAddon<AtkUnitBase>()->AtkValuesSpan;
        public AtkEventData.AtkMouseData* MouseData => args.GetMouseData();
        public Vector2 ClickPosition => args.GetMouseClickPosition();

        public T* GetAddon<T>() where T : unmanaged => (T*)args.Addon.Address;

        public void PrintAtkValues() {
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

        private AtkEventData.AtkMouseData* GetMouseData() {
            if (args is not AddonReceiveEventArgs eventArgs) return null;

            return &((AtkEventData*)eventArgs.AtkEventData)->MouseData;
        }

        private Vector2 GetMouseClickPosition() {
            var mouseData = GetMouseData(args);
            if (mouseData is null) return Vector2.Zero;

            return new Vector2(mouseData->PosX, mouseData->PosY);
        }
    }
}
