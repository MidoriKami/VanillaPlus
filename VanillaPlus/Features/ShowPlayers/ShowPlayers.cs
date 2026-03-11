using System.Linq;
using KamiToolKit.Overlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowPlayers;

public class ShowPlayersOnMap : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Show Players",
        Description = "Shows icons for other players on the map.",
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatibilityModule = new PluginCompatibilityModule("Mappy"),
    };

    public override string ImageName => "ShowPlayers.png";

    private MapOverlayController? mapOverlayController;

    public override void OnEnable() {
        mapOverlayController = new MapOverlayController();

        Services.Framework.RunOnFrameworkThread(() => {
            foreach (var index in Enumerable.Range(1, 99)) {
                mapOverlayController.AddMarker(new PlayerMapMarker {
                    PlayerIndex = index,
                });
            }
        });
    }

    public override void OnDisable() {
        mapOverlayController?.Dispose();
        mapOverlayController = null;
    }
}
