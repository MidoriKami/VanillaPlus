using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.FasterScroll;

public class FasterScroll : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FasterScroll,
        Description = Strings.ModificationDescription_FasterScroll,
        Authors = ["MidoriKami"],
        Type = ModificationType.GameBehavior,
    };

    private Hook<AtkComponentScrollBar.Delegates.ReceiveEvent>? scrollBarReceiveEventHook;

    private FasterScrollConfig? config;
    private ConfigAddon? configWindow;

    public override async Task OnEnableAsync() {
        config = await FasterScrollConfig.Load();

        config.SpeedMultiplier = MathF.Round(config.SpeedMultiplier, 1);

        configWindow = new ConfigAddon {
            Size = new Vector2(400.0f, 125.0f),
            InternalName = "FasterScrollConfig",
            Title = Strings.FasterScroll_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.Settings)
            .AddFloatSlider(Strings.FasterScroll_LabelSpeedMultiplier, 0.5f, 4.0f, 0.1f, nameof(config.SpeedMultiplier));

        OpenConfigAction = configWindow.Toggle;

        unsafe {
            scrollBarReceiveEventHook = IGameInteropProvider.Get().HookFromAddress<AtkComponentScrollBar.Delegates.ReceiveEvent>(AtkComponentScrollBar.StaticVirtualTablePointer->ReceiveEvent, AtkComponentScrollBarReceiveEvent);
        }

        await scrollBarReceiveEventHook.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        await scrollBarReceiveEventHook.DisposeAsync();
        scrollBarReceiveEventHook = null;

        await configWindow.DisposeAsyncSafe();
        configWindow = null;

        config = null;
    }

    private unsafe void AtkComponentScrollBarReceiveEvent(AtkComponentScrollBar* thisPtr, AtkEventType type, int param, AtkEvent* eventPointer, AtkEventData* dataPointer) {
        try {
            if (config is null) {
                scrollBarReceiveEventHook!.Original(thisPtr, type, param, eventPointer, dataPointer);
                return;
            }

            thisPtr->MouseWheelSpeed = (short)(config.SpeedMultiplier * thisPtr->MouseWheelSpeed);
            scrollBarReceiveEventHook!.Original(thisPtr, type, param, eventPointer, dataPointer);
            thisPtr->MouseWheelSpeed = (short)(thisPtr->MouseWheelSpeed / config.SpeedMultiplier);
        }
        catch (Exception e) {
            IPluginLog.Get().Exception(e);
        }
    }
}
