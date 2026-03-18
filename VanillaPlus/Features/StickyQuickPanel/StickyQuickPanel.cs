using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.StickyQuickPanel;

public unsafe class StickyQuickPanel : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Sticky Command Panel",
        Description = "Prevents the Command Panel from closing during load screens.",
        Type = ModificationType.GameBehavior,
        Authors = [ "Treezy" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private delegate void CloseAddonsOnTeleportDelegate(ulong a1);
    [Signature("40 56 48 83 EC ?? 80 B9 ?? ?? ?? ?? ?? 48 8B F1 74 ?? 81 3D", DetourName = nameof(CloseAddonsOnTeleportDetour))]
    private Hook<CloseAddonsOnTeleportDelegate> closeAddonsOnTeleportHook = null!;

    private delegate void CloseAddonOnWipeDelegate(RaptureAtkUnitManager* thisPtr, AtkUnitBase* addonBase, bool a3, byte close, bool a5);
    [Signature("40 53 48 83 EC ?? 8B 8A", DetourName = nameof(CloseAddonOnWipeDetour))]
    private Hook<CloseAddonOnWipeDelegate> closeAddonOnWipeHook = null!;

    private Hook<AtkUnitBase.Delegates.FireCallback> fireCallBackHook = null!;

    public override void OnEnable() {
        try {
            Services.GameInteropProvider.InitializeFromAttributes(this);
            fireCallBackHook = Services.GameInteropProvider.HookFromAddress(
                AtkUnitBase.Addresses.FireCallback.Value, new AtkUnitBase.Delegates.FireCallback(FireCallbackDetour)
            );
            closeAddonsOnTeleportHook.Enable();
            closeAddonOnWipeHook.Enable();
        }
        catch (Exception e) {
            Services.PluginLog.Error($"StickyQuickPanel failed to initialise hooks: {e.Message}");
            OnDisable();
        }
    }

    public override void OnDisable() {
        closeAddonsOnTeleportHook.Dispose();
        closeAddonOnWipeHook.Dispose();
        fireCallBackHook.Dispose();
    }

    private void CloseAddonsOnTeleportDetour(ulong a1) {
        fireCallBackHook.Enable();
        closeAddonsOnTeleportHook.Original(a1);
        fireCallBackHook.Disable();
    }

    private void CloseAddonOnWipeDetour(RaptureAtkUnitManager* thisPtr, AtkUnitBase* addonBase, bool a3, byte close, bool a5) {
        fireCallBackHook.Enable();
        closeAddonOnWipeHook.Original(thisPtr, addonBase, a3, close, a5);
        fireCallBackHook.Disable();
    }

    private bool FireCallbackDetour(AtkUnitBase* thisPtr, uint valueCount, AtkValue* values, bool close) {
        if (thisPtr != null && thisPtr->NameString == "QuickPanel") return true;
        return fireCallBackHook.Original(thisPtr, valueCount, values, close);
    }
}