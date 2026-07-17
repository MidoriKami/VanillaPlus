using System;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Features.AprilFools;

/// <summary>
/// With the default setting of Invert Scroll, will make mouse scrolls reverse direction for scrollbars in game.
/// Clicking and dragging a scrollbar is uneffected.
///
/// When in Insane Scroll mode which the user has to explicitly opt in, reverses the scroll direction every other second,
/// with even seconds scrolling backwards twice as fast as forwards.
/// </summary>
public class ScrollingFools : FoolsModule {
    private Hook<AtkComponentScrollBar.Delegates.ReceiveEvent>? scrollBarReceiveEventHook;

    public override bool IsEnabledByConfig
        => Config.InvertScroll;

    protected override Task OnEnable() {
        unsafe {
            scrollBarReceiveEventHook = Service<IGameInteropProvider>.Get().HookFromAddress<AtkComponentScrollBar.Delegates.ReceiveEvent>(AtkComponentScrollBar.StaticVirtualTablePointer->ReceiveEvent, AtkComponentScrollBarReceiveEvent);
            scrollBarReceiveEventHook?.Enable();
        }

        return Task.CompletedTask;
    }

    protected override Task OnDisable() {
        scrollBarReceiveEventHook?.Dispose();
        scrollBarReceiveEventHook = null;

        return Task.CompletedTask;
    }

    private unsafe void AtkComponentScrollBarReceiveEvent(AtkComponentScrollBar* thisPtr, AtkEventType type, int param, AtkEvent* eventPointer, AtkEventData* dataPointer) {
        try {
            dataPointer->MouseData.WheelDirection *= -1;
            scrollBarReceiveEventHook!.Original(thisPtr, type, param, eventPointer, dataPointer);
        }
        catch (Exception e) {
            Service<IPluginLog>.Get().Exception(e);
        }
    }
}
