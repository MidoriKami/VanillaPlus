using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using KamiToolKit.Overlay.MapOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowPlayers;

public unsafe class ShowPlayersOnMap : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ShowPlayers,
        Description = Strings.ModificationDescription_ShowPlayers,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = [ "MidoriKami" ],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    public override string ImageName => "ShowPlayers.png";

    private MapOverlayController? mapOverlayController;

    public override void OnEnable() {
        mapOverlayController = new MapOverlayController();

        Services.Framework.RunOnFrameworkThread(() => {
            foreach (var index in Enumerable.Range(0, CharacterManager.Instance()->BattleCharas.Length)) {
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
