using System;
using Dalamud.Game.ClientState.Fates;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Extensions;

public static unsafe class FateExtensions {
    extension(IFate fate) {
        public void FocusMarker() {
            var agentMap = AgentMap.Instance();
            if (agentMap is not null) {
                agentMap->FlagMarkerCount = 0;
                agentMap->SetFlagMapMarker(agentMap->CurrentTerritoryId, agentMap->CurrentMapId, fate.Position, fate.IconId);
                agentMap->OpenMap(agentMap->CurrentMapId, agentMap->CurrentTerritoryId, fate.Name.ToString(), MapType.QuestLog);
                agentMap->OpenMap(agentMap->CurrentMapId, agentMap->CurrentTerritoryId, fate.Name.ToString());
            }
        }

        public TimeSpan TimeRemainingSpan => TimeSpan.FromSeconds(fate.TimeRemaining);
        public string TimeRemainingString => fate.TimeRemainingSpan.ToString(@"mm\:ss");
        public ReadOnlySeString NameString => fate.Name.EncodeWithNullTerminator();
    }
}
