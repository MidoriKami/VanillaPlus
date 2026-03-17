using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using KamiToolKit.Overlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowEnemies;

public unsafe class ShowEnemies : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Show Enemies",
        Description = "Shows icons for enemies players on the map.",
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    public override string ImageName => "ShowEnemies.png";

    private MapOverlayController? mapOverlayController;

    public override void OnEnable() {
        mapOverlayController = new MapOverlayController();

        Services.Framework.RunOnFrameworkThread(() => {
            foreach (var index in Enumerable.Range(0, CharacterManager.Instance()->BattleCharas.Length)) {
                mapOverlayController.AddMarker(new EnemyMapMarker {
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
