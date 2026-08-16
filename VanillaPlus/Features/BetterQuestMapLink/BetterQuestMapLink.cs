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

public class BetterQuestMapLink : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_BetterQuestMapLink,
        Description = Strings.ModificationDescription_BetterQuestMapLink,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("Mappy"),
    };

    private Hook<AgentMap.Delegates.OpenMap>? openMapHook;

    public override async Task OnEnableAsync() {
        unsafe {
            openMapHook = IGameInteropProvider.Get().HookFromAddress<AgentMap.Delegates.OpenMap>(AgentMap.MemberFunctionPointers.OpenMap, OnOpenMap);
        }

        await openMapHook.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        await openMapHook.DisposeAsync();
        openMapHook = null;
    }

    private unsafe void OnOpenMap(AgentMap* agent, OpenMapInfo* data) {
        openMapHook!.Original(agent, data);

        try {
            if (!IDataManager.Get().GetExcelSheet<Map>().TryGetRow(data->MapId, out var mapData)) return;

            // Disable in Cosmic Zones
            if (mapData.TerritoryType.ValueNullable?.TerritoryIntendedUse.RowId is 60) return;

            if (data->Type is MapType.QuestLog && agent->CurrentMapId != data->MapId) {
                data->Type = MapType.Centered;
                data->TerritoryId = 0;
                openMapHook!.Original(agent, data);
            }
        }
        catch (Exception e) {
            IPluginLog.Get().Exception(e);
        }
    }
}
