using System.Linq;
using KamiToolKit.Overlay.MapOverlay;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowAetherCurrents;

public class ShowAetherCurrents : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ShowAetherCurrents,
        Description = Strings.ModificationDescription_ShowAetherCurrents,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = [ "MidoriKami" ],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    public override string ImageName => "ShowAetherCurrents.png";

    private MapOverlayController? mapOverlayController;

    public override void OnEnable() {
        mapOverlayController = new MapOverlayController();

        Services.Framework.RunOnFrameworkThread(AddAetherCurrents);
    }

    public override void OnDisable() {
        mapOverlayController?.Dispose();
        mapOverlayController = null;
    }

    private void AddAetherCurrents() {
        if (mapOverlayController is null) return;

        var aetherCurrents = Services.DataManager
            .GetExcelSheet<AetherCurrentCompFlgSet>()
            .SelectMany(currentSet => currentSet.AetherCurrents);

        foreach (var aetherCurrent in aetherCurrents) {
            if (!aetherCurrent.IsValid) continue;

            // Skip any aether currents that require quests as they don't need to be shown on the map.
            if (aetherCurrent.Value.Quest.IsValid) continue;

            mapOverlayController.AddMarker(new AetherCurrentMapMarker {
                AetherCurrent = aetherCurrent,
            });
        }
    }
}
