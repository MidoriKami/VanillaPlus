using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.SkipTeleportConfirm;

public unsafe class SkipTeleportConfirm : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Skip Teleport Confirm",
        Description = "Skips the 'Teleport to [Location] for [amount] gil?' popup when using the map to teleport.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private Hook<AgentInterface.Delegates.ReceiveEvent>? mapReceiveEventHook;

    public override void OnEnable() {
        mapReceiveEventHook = Services.Hooker.HookFromAddress<AgentInterface.Delegates.ReceiveEvent>(AgentMap.Instance()->VirtualTable->ReceiveEvent, OnAgentMapReceiveEvent);
        mapReceiveEventHook?.Enable();
    }

    public override void OnDisable() {
        mapReceiveEventHook?.Dispose();
        mapReceiveEventHook = null;
    }

    private AtkValue* OnAgentMapReceiveEvent(AgentInterface* thisPtr, AtkValue* returnValue, AtkValue* values, uint valueCount, ulong eventKind) {
        var result = mapReceiveEventHook!.Original(thisPtr, returnValue, values, valueCount, eventKind);

        try {
            if (valueCount is 2 && values[0].Int is 7) {
                var addon = Services.GameGui.GetAddonByName<AddonSelectYesno>("SelectYesno");
                if (addon is not null) {
                    var newValues = stackalloc AtkValue[1];
                    newValues->SetInt(0);

                    addon->FireCallback(1, newValues, true);
                }
            }
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Exception in OnAgentMapReceiveEvent");
        }

        return result;
    }
}
