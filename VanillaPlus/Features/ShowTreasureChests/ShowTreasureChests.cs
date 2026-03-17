using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using KamiToolKit.Overlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowTreasureChests;

public unsafe class ShowTreasureChests : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Show Treasure Chests",
        Description = "Shows icons for treasure chests on the map.",
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    public override string ImageName => "ShowTreasureChests.png";

    private MapOverlayController? mapOverlayController;

    public override void OnEnable() {
        mapOverlayController = new MapOverlayController();

        Services.Framework.RunOnFrameworkThread(() => {
            foreach (var index in Enumerable.Range(0, EventObjectManager.Instance()->EventObjects.Length)) {
                mapOverlayController.AddMarker(new TreasureChestMapMarker {
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
