using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using CsAgentId = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentId;
using AgentId = Dalamud.Game.Agent.AgentId;

namespace VanillaPlus.Features.SkipTeleportConfirm;

public unsafe class SkipTeleportConfirm : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SkipTeleportConfirm,
        Description = Strings.ModificationDescription_SkipTeleportConfirm,
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override void OnEnable()
        => Services.AgentLifecycle.RegisterListener(AgentEvent.PreReceiveEvent, AgentId.Map, OnAgentMapReceiveEvent);

    public override void OnDisable()
        => Services.AgentLifecycle.UnregisterListener(OnAgentMapReceiveEvent);

    private static void OnAgentMapReceiveEvent(AgentEvent type, AgentArgs args) {
        if (args is not AgentReceiveEventArgs receiveEventArgs) return;

        var valueCount = (int) receiveEventArgs.ValueCount;
        var valueSpan = new Span<AtkValue>((AtkValue*)receiveEventArgs.AtkValues, valueCount);

        if (receiveEventArgs.ValueCount is 2 && valueSpan[0].Int is 7) {
            Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", SelectYesNoHandler);
        }
    }

    private static void SelectYesNoHandler(AddonEvent _, AddonArgs yesNoArgs) {
        var addon = yesNoArgs.GetAddon<AddonSelectYesno>();

        if (addon->AtkUnitBase.GetCallbackHandlerInfo() is { AgentId: CsAgentId.Map, EventKind: 1 }) {
            var newValues = stackalloc AtkValue[1];
            newValues->SetInt(0);

            addon->FireCallback(1, newValues, true);
        }

        Services.AddonLifecycle.UnregisterListener(SelectYesNoHandler);
    }
}
