using System.Linq;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using KamiToolKit.MapOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowPlayers;

public class ShowPlayersOnMap : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ShowPlayers,
        Description = Strings.ModificationDescription_ShowPlayers,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = ["MidoriKami"],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    public override string ImageName => "ShowPlayers.png";

    private MapOverlayController? mapOverlayController;

    public override async Task OnEnableAsync() {
        mapOverlayController = new MapOverlayController();

        await Services.GetService<IFramework>().RunSafely(() => {
            unsafe {
                foreach (var index in Enumerable.Range(0, CharacterManager.Instance()->BattleCharas.Length)) {
                    mapOverlayController.AddMarker(new PlayerMapMarker {
                        PlayerIndex = index,
                    });
                }
            }

            mapOverlayController.Enable();
        });
    }

    public override async Task OnDisableAsync() {
        await Services.GetService<IFramework>().RunSafely(() => mapOverlayController?.Dispose());
        mapOverlayController = null;
    }
}
