using System.Linq;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using KamiToolKit.MapOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowEnemies;

public class ShowEnemies : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ShowEnemies,
        Description = Strings.ModificationDescription_ShowEnemies,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = ["MidoriKami"],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    public override string ImageName => "ShowEnemies.png";

    private MapOverlayController? mapOverlayController;

    public override async Task OnEnableAsync() {
        mapOverlayController = new MapOverlayController();

        await Service<IFramework>.Get().RunSafely(() => {
            unsafe {
                foreach (var index in Enumerable.Range(0, CharacterManager.Instance()->BattleCharas.Length)) {
                    mapOverlayController.AddMarker(new EnemyMapMarker {
                        ObjectIndex = index,
                    });
                }
            }

            mapOverlayController.Enable();
        });
    }

    public override async Task OnDisableAsync() {
        await Service<IFramework>.Get().RunSafely(() => mapOverlayController?.Dispose());
        mapOverlayController = null;
    }
}
