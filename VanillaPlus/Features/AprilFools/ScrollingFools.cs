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

    protected override async Task OnEnable() {
        unsafe {
            scrollBarReceiveEventHook = IGameInteropProvider.Get().HookFromAddress<AtkComponentScrollBar.Delegates.ReceiveEvent>(AtkComponentScrollBar.StaticVirtualTablePointer->ReceiveEvent, AtkComponentScrollBarReceiveEvent);
        }

        await scrollBarReceiveEventHook.EnableAsync();
    }

    protected override async Task OnDisable() {
        await scrollBarReceiveEventHook.DisposeAsync();
        scrollBarReceiveEventHook = null;
    }

    private unsafe void AtkComponentScrollBarReceiveEvent(AtkComponentScrollBar* thisPtr, AtkEventType type, int param, AtkEvent* eventPointer, AtkEventData* dataPointer) {
        try {
            dataPointer->MouseData.WheelDirection *= -1;
            scrollBarReceiveEventHook!.Original(thisPtr, type, param, eventPointer, dataPointer);
        }
        catch (Exception e) {
            IPluginLog.Get().Exception(e);
        }
    }
}
