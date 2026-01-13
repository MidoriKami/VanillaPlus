using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using MapType = FFXIVClientStructs.FFXIV.Client.UI.Agent.MapType;

namespace VanillaPlus.Extensions;

public static unsafe class MarkerInfoExtensions {
    extension(MarkerInfo markerInfo) {
        public void FocusMarker() {
            var agentMap = AgentMap.Instance();
            if (agentMap is not null) {
                var position = markerInfo.MarkerData.First->Position;
                var icon = markerInfo.MarkerData.First->IconId;
                var name = markerInfo.Label.ToString();

                agentMap->FlagMarkerCount = 0;
                agentMap->SetFlagMapMarker(agentMap->CurrentTerritoryId, agentMap->CurrentMapId, position, icon);
                agentMap->OpenMap(agentMap->CurrentMapId, agentMap->CurrentTerritoryId, name, MapType.QuestLog );
                agentMap->OpenMap(agentMap->CurrentMapId, agentMap->CurrentTerritoryId, name);
            }
        }

        public string Name => markerInfo.Label.ToString();
       
        public uint IconId => markerInfo.MarkerData.First->IconId;
        
        public uint ClassJobLevel => Services.DataManager.GetExcelSheet<Quest>().GetRow(markerInfo.ObjectiveId + ushort.MaxValue + 1).ClassJobLevel.First();
        
        public float Distance {
            get {
                if (markerInfo.MarkerData.Count <= 0) return 0;
                
                return Vector3.Distance(Services.ObjectTable.LocalPlayer?.Position ?? Vector3.Zero, markerInfo.MarkerData.First->Position);
            }
        }

        public string IssuerName 
            => Services.DataManager.GetExcelSheet<Quest>().GetRow(markerInfo.ObjectiveId + ushort.MaxValue + 1)
                   .IssuerStart.GetValueOrDefault<ENpcResident>()?.Singular.ToString() ?? string.Empty;
    }
}
