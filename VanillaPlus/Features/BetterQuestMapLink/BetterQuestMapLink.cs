using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using MapType = FFXIVClientStructs.FFXIV.Client.UI.Agent.MapType;

namespace VanillaPlus.Features.BetterQuestMapLink;

public unsafe class BetterQuestMapLink : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_BetterQuestMapLink,
        Description = Strings.ModificationDescription_BetterQuestMapLink,
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
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
            if (!Services.DataManager.GetExcelSheet<Map>().TryGetRow(data->MapId, out var mapData)) return;

            // Disable in Cosmic Zones
            if (mapData.TerritoryType.ValueNullable?.TerritoryIntendedUse.RowId is 60) return; 

            if (data->Type is MapType.QuestLog && agent->CurrentMapId != data->MapId) {
                data->Type = MapType.Centered;
                data->TerritoryId = 0;
                openMapHook!.Original(agent, data);
            }
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Exception while opening map");
        }
    }
}
