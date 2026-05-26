using System.Collections.Generic;
using System.Threading.Tasks;
using KamiToolKit.Overlay.MapOverlay;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowAetherCurrents;

public class ShowAetherCurrents : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ShowAetherCurrents,
        Description = Strings.ModificationDescription_ShowAetherCurrents,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = ["MidoriKami"],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    public override string ImageName => "ShowAetherCurrents.png";

    private MapOverlayController? mapOverlayController;

    public override async Task OnEnableAsync() {
        mapOverlayController = new MapOverlayController();

        List<AetherCurrentInfo> aetherCurrentInfos = [];

        foreach (var currentFlagSet in Services.DataManager.GetExcelSheet<AetherCurrentCompFlgSet>()) {
            foreach (var aetherCurrent in currentFlagSet.AetherCurrents) {
                if (aetherCurrent is not { IsValid: true, Value.Quest.IsValid: true }) continue;

                if (!Services.DataManager.GetExcelSheet<EObj>().TryGetFirst(rowObject => rowObject.Data.RowId == aetherCurrent.RowId, out var eventObject)) continue;
                if (!Services.DataManager.GetExcelSheet<Level>().TryGetFirst(rowObject => rowObject.Object.RowId == eventObject.RowId, out var level)) continue;

                aetherCurrentInfos.Add(new AetherCurrentInfo {
                    RowData = aetherCurrent,
                    LevelData = level,
                });
            }
        }

        await Services.Framework.Run(() => {
            foreach (var aetherCurrent in aetherCurrentInfos) {
                mapOverlayController.AddMarker(new AetherCurrentMapMarker {
                    AetherCurrent = aetherCurrent.RowData,
                    MapId = aetherCurrent.MapId,
                    Position = aetherCurrent.Position,
                });
            }

            mapOverlayController.Enable();
        });
    }

    public override async Task OnDisableAsync() {
        if (mapOverlayController is not null) {
            await mapOverlayController.DisableAsync();
            mapOverlayController = null;
        }
    }
}
