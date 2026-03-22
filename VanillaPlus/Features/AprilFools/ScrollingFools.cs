using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Features.AprilFools;

public unsafe class ScrollingFools : IFoolsModule {
    public required AprilFoolsConfig Config { get; set; }

    private Hook<AtkComponentScrollBar.Delegates.ReceiveEvent>? scrollBarReceiveEventHook;
    
    public void Enable() {
        scrollBarReceiveEventHook = Services.Hooker.HookFromAddress<AtkComponentScrollBar.Delegates.ReceiveEvent>(AtkComponentScrollBar.StaticVirtualTablePointer->ReceiveEvent, AtkComponentScrollBarReceiveEvent);
        scrollBarReceiveEventHook?.Enable();
    }

    public void Disable() {
        scrollBarReceiveEventHook?.Dispose();
        scrollBarReceiveEventHook = null;
    }
    
    private void AtkComponentScrollBarReceiveEvent(AtkComponentScrollBar* thisPtr, AtkEventType type, int param, AtkEvent* eventPointer, AtkEventData* dataPointer) {
        try {
            if (Config.InsaneScrollMode) {
                dataPointer->MouseData.WheelDirection *= (short) (DateTime.UtcNow.Second % 2 is 0 ? -2 : 1);
            }
            else if (Config.InvertScroll) {
                dataPointer->MouseData.WheelDirection *= -1;
            }
        
            scrollBarReceiveEventHook!.Original(thisPtr, type, param, eventPointer, dataPointer);
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Error in AtkComponentScrollBarReceiveEvent");
        }
    }
}
