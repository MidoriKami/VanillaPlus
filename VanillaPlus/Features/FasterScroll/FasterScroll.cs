using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.FasterScroll;

public class FasterScroll : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Faster Scrollbars",
        Description = "Increases the speed of all scrollbars.",
        Authors = ["MidoriKami"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    [Signature("40 55 53 56 41 54 41 55 41 56 41 57 48 8B EC 48 81 EC ?? ?? ?? ??", DetourName = nameof(AtkComponentScrollBarReceiveEvent))]
    private readonly Hook<AtkComponentScrollBar.Delegates.ReceiveEvent>? scrollBarReceiveEventHook = null;

    private FasterScrollConfig config = null!;
    private FasterScrollConfigWindow configWindow = null!;

    public override void OnEnable() {
        config = FasterScrollConfig.Load();
        configWindow = new FasterScrollConfigWindow(config);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;

        Services.GameInteropProvider.InitializeFromAttributes(this);
        scrollBarReceiveEventHook?.Enable();
    }

    public override void OnDisable() {
        scrollBarReceiveEventHook?.Dispose();
        configWindow.RemoveFromWindowSystem();
    }

    private unsafe void AtkComponentScrollBarReceiveEvent(AtkComponentScrollBar* thisPtr, AtkEventType type, int param, AtkEvent* eventPointer, AtkEventData* dataPointer) {
        thisPtr->MouseWheelSpeed = (short) ( config.SpeedMultiplier * thisPtr->MouseWheelSpeed );
        scrollBarReceiveEventHook!.Original(thisPtr, type, param, eventPointer, dataPointer);
        thisPtr->MouseWheelSpeed = (short) ( thisPtr->MouseWheelSpeed / config.SpeedMultiplier );
    }
}
