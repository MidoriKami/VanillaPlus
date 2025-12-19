using System.Numerics;
using Dalamud.Game.Addon.Events.EventDataTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ModifierFlag = FFXIVClientStructs.FFXIV.Component.GUI.AtkEventData.AtkMouseData.ModifierFlag;

namespace VanillaPlus.Extensions;

public static unsafe class AddonEventDataExtensions {
    extension(AddonEventData data) {
        public void SetHandled(bool forced = true) => data.Event->SetEventIsHandled(forced);
        public ref AtkEventData.AtkMouseData MouseData => ref data.EventData->MouseData;
        public ref AtkEventData.AtkDragDropData DragDropData => ref data.EventData->DragDropData;
        public bool IsLeftClick => data.MouseData.ButtonId is 0;
        public bool IsRightClick => data.MouseData.ButtonId is 1;
        public bool IsNoModifier => data.MouseData.Modifier is 0;
        public bool IsAltHeld => data.MouseData.Modifier.HasFlag(ModifierFlag.Alt);
        public bool IsControlHeld => data.MouseData.Modifier.HasFlag(ModifierFlag.Ctrl);
        public bool IsShiftHeld => data.MouseData.Modifier.HasFlag(ModifierFlag.Shift);
        public bool IsDragging => data.MouseData.Modifier.HasFlag(ModifierFlag.Dragging);
        public bool IsScrollUp => data.MouseData.WheelDirection is 1;
        public bool IsScrollDown => data.MouseData.WheelDirection is -1;
        public Vector2 MousePosition => new(data.MouseData.PosX, data.MouseData.PosY);
        private AtkEvent* Event => (AtkEvent*)data.AtkEventPointer;
        private AtkEventData* EventData => (AtkEventData*)data.AtkEventDataPointer;
    }
}
