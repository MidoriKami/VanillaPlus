using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Features.AprilFools;

/// <summary>
/// With the default setting of Invert Scroll, will make mouse scrolls reverse direction for scrollbars in game.
/// Clicking and dragging a scrollbar is uneffected.
///
/// When in Insane Scroll mode which the user has to explicitly opt in, reverses the scroll direction every other second,
/// with even seconds scrolling backwards twice as fast as forwards.
/// </summary>
public unsafe class ScrollingFools : FoolsModule {
    private Hook<AtkComponentScrollBar.Delegates.ReceiveEvent>? scrollBarReceiveEventHook;
    
    public override bool IsEnabledByConfig 
        => Config.InvertScroll;
    
    protected override void OnEnable() {
        scrollBarReceiveEventHook = Services.Hooker.HookFromAddress<AtkComponentScrollBar.Delegates.ReceiveEvent>(AtkComponentScrollBar.StaticVirtualTablePointer->ReceiveEvent, AtkComponentScrollBarReceiveEvent);
        scrollBarReceiveEventHook?.Enable();
    }

    protected override void OnDisable() {
        scrollBarReceiveEventHook?.Dispose();
        scrollBarReceiveEventHook = null;
    }
    
    private void AtkComponentScrollBarReceiveEvent(AtkComponentScrollBar* thisPtr, AtkEventType type, int param, AtkEvent* eventPointer, AtkEventData* dataPointer) {
        try {
            dataPointer->MouseData.WheelDirection *= -1;
            scrollBarReceiveEventHook!.Original(thisPtr, type, param, eventPointer, dataPointer);
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Error in AtkComponentScrollBarReceiveEvent");
        }
    }
}
