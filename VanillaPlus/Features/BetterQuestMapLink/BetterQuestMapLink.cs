using System;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
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
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("Mappy"),
    };

    private Hook<AgentMap.Delegates.OpenMap>? openMapHook;

    public override Task OnEnableAsync() {
        openMapHook = Service<IGameInteropProvider>.Get().HookFromAddress<AgentMap.Delegates.OpenMap>(AgentMap.MemberFunctionPointers.OpenMap, OnOpenMap);
        openMapHook?.Enable();

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        openMapHook?.Dispose();
        openMapHook = null;

        return Task.CompletedTask;
    }

    private void OnOpenMap(AgentMap* agent, OpenMapInfo* data) {
        openMapHook!.Original(agent, data);

        try {
            if (!Service<IDataManager>.Get().GetExcelSheet<Map>().TryGetRow(data->MapId, out var mapData)) return;

            // Disable in Cosmic Zones
            if (mapData.TerritoryType.ValueNullable?.TerritoryIntendedUse.RowId is 60) return;

            if (data->Type is MapType.QuestLog && agent->CurrentMapId != data->MapId) {
                data->Type = MapType.Centered;
                data->TerritoryId = 0;
                openMapHook!.Original(agent, data);
            }
        }
        catch (Exception e) {
            Service<IPluginLog>.Get().Exception(e);
        }
    }
}
