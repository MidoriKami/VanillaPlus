using System;
using System.Linq;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ShowAetherCurrents;

public unsafe class ShowAetherCurrents : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Show Aether Currents",
        Description = "Shows all available aether currents on the map.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "ShowAetherCurrents.png";

    private Utf8String* tooltipString;

    private delegate void CreateMapMarkersDelegate(AgentMap* instance, bool omitAetherytes);

    [Signature("E8 ?? ?? ?? ?? 8B 8D ?? ?? ?? ?? 83 E1 FD", DetourName = nameof(CreateMapMarkers))]
    private Hook<CreateMapMarkersDelegate>? populateMapMarkersHook;

    public override void OnEnable() {
        Services.Hooker.InitializeFromAttributes(this);
        populateMapMarkersHook?.Enable();

        Services.Framework.RunOnFrameworkThread(() => {
            tooltipString = Utf8String.FromString("Aether Current");
            
            AgentMap.Instance()->ResetMapMarkers();
        });
    }

    public override void OnDisable() {
        populateMapMarkersHook?.Dispose();
        populateMapMarkersHook = null;

        AgentMap.Instance()->ResetMapMarkers();

        if (tooltipString is not null) {
            tooltipString->Dtor(true);
            tooltipString = null;
        }
    }

    private void CreateMapMarkers(AgentMap* thisPtr, bool omitAetherytes) {

        populateMapMarkersHook!.Original(thisPtr, omitAetherytes);
        if (omitAetherytes) return;
        if (tooltipString is null) return;

        try {
            var aetherCurrents = Services.DataManager
                .GetExcelSheet<AetherCurrentCompFlgSet>()
                .Where(aetherCurrentSet => aetherCurrentSet.Territory.RowId == thisPtr->SelectedTerritoryId)
                .SelectMany(currentSet => currentSet.AetherCurrents);

            foreach (var aetherCurrent in aetherCurrents) {
                if (!aetherCurrent.IsValid) continue;

                // Skip any aether currents that require quests as they don't need to be shown on the map.
                if (aetherCurrent.Value.Quest.IsValid) continue;

                // Skip any that are already unlocked
                if (PlayerState.Instance()->IsAetherCurrentUnlocked(aetherCurrent.RowId)) continue;

                if (!Services.DataManager.GetExcelSheet<EObj>().TryGetFirst(rowObject => rowObject.Data == aetherCurrent.RowId, out var eventObject)) continue;
                if (!Services.DataManager.GetExcelSheet<Level>().TryGetFirst(rowObject => rowObject.Object.RowId == eventObject.RowId, out var level)) continue;

                thisPtr->AddMapMarker(new Vector3(level.X, level.Y, level.Z), 60653, 0, tooltipString->StringPtr);
            }
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Exception while trying to add Aether Currents to Map");
        }
    }
}
