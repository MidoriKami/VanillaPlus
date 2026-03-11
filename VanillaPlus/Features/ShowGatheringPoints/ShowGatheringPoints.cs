using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using KamiToolKit.Overlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowGatheringPoints;

public unsafe class ShowGatheringPoints : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Show Gathering Points",
        Description = "Shows icons for gathering points on the map.",
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "ShowGatheringPoint.png";

    private MapOverlayController? mapOverlayController;

    public override void OnEnable() {
        mapOverlayController = new MapOverlayController();

        Services.Framework.RunOnFrameworkThread(() => {
            foreach (var index in Enumerable.Range(0, EventObjectManager.Instance()->EventObjects.Length)) {
                mapOverlayController.AddMarker(new GatheringPointMapMarker {
                    ObjectIndex = index,
                });
            }
        });
    }

    public override void OnDisable() {
        mapOverlayController?.Dispose();
        mapOverlayController = null;
    }
}
