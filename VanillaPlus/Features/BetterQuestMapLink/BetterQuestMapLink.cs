using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.BetterQuestMapLink;

public unsafe class BetterQuestMapLink : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Better Quest Map Link",
        Description = "When clicking on quest links, open the actual map the quest is for instead of the generic world map.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
        CompatibilityModule = new PluginCompatibilityModule("Mappy"),
    };

    private Hook<AgentMap.Delegates.OpenMap>? openMapHook;
    
    public override void OnEnable() {
        openMapHook = Services.Hooker.HookFromAddress<AgentMap.Delegates.OpenMap>(AgentMap.MemberFunctionPointers.OpenMap, OnOpenMap);
        openMapHook?.Enable();
    }

    public override void OnDisable() {
        openMapHook?.Dispose();
        openMapHook = null;
    }

    private void OnOpenMap(AgentMap* agent, OpenMapInfo* data) {
        openMapHook!.Original(agent, data);
        
        try {
            if (data->Type is MapType.QuestLog) {
                data->Type = MapType.Centered;
                data->TerritoryId = 0;
                OnOpenMap(agent, data);
            }
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Exception while opening map");
        }
    }
}
