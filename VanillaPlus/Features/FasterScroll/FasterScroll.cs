using System;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.FasterScroll;

public unsafe class FasterScroll : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_FasterScroll"),
        Description = Strings("ModificationDescription_FasterScroll"),
        Authors = ["MidoriKami"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private Hook<AtkComponentScrollBar.Delegates.ReceiveEvent>? scrollBarReceiveEventHook;

    private FasterScrollConfig? config;
    private ConfigAddon? configWindow;

    public override void OnEnable() {
        config = FasterScrollConfig.Load();

        configWindow = new ConfigAddon {
            Size = new Vector2(400.0f, 125.0f),
            InternalName = "FasterScrollConfig",
            Title = Strings("FasterScroll_ConfigTitle"),
            Config = config,
        };

        configWindow.AddCategory(Strings("FasterScroll_CategorySettings"))
            .AddFloatSlider(Strings("FasterScroll_LabelSpeedMultiplier"), 0.5f, 4.0f, 2, 0.05f, nameof(config.SpeedMultiplier));
        
        OpenConfigAction = configWindow.Toggle;

        scrollBarReceiveEventHook = Services.Hooker.HookFromAddress<AtkComponentScrollBar.Delegates.ReceiveEvent>(AtkComponentScrollBar.StaticVirtualTablePointer->ReceiveEvent, AtkComponentScrollBarReceiveEvent);
        scrollBarReceiveEventHook?.Enable();
    }

    public override void OnDisable() {
        scrollBarReceiveEventHook?.Dispose();
        scrollBarReceiveEventHook = null;
        
        configWindow?.Dispose();
        configWindow = null;

        config = null;
    }

    private void AtkComponentScrollBarReceiveEvent(AtkComponentScrollBar* thisPtr, AtkEventType type, int param, AtkEvent* eventPointer, AtkEventData* dataPointer) {
        try {
            if (config is null) {
                scrollBarReceiveEventHook!.Original(thisPtr, type, param, eventPointer, dataPointer);
                return;
            }
        
            thisPtr->MouseWheelSpeed = (short) ( config.SpeedMultiplier * thisPtr->MouseWheelSpeed );
            scrollBarReceiveEventHook!.Original(thisPtr, type, param, eventPointer, dataPointer);
            thisPtr->MouseWheelSpeed = (short) ( thisPtr->MouseWheelSpeed / config.SpeedMultiplier );
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Error in AtkComponentScrollBarReceiveEvent");
        }
    }
}
