using System.Linq;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using KamiToolKit.MapOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowTreasureChests;

public class ShowTreasureChests : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ShowTreasureChests,
        Description = Strings.ModificationDescription_ShowTreasureChests,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = ["MidoriKami"],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    public override string ImageName => "ShowTreasureChests.png";

    private MapOverlayController? mapOverlayController;

    public override async Task OnEnableAsync() {
        mapOverlayController = new MapOverlayController();

        await Services.GetService<IFramework>().RunSafely(() => {
            unsafe {
                foreach (var index in Enumerable.Range(0, EventObjectManager.Instance()->EventObjects.Length)) {
                    mapOverlayController.AddMarker(new TreasureChestMapMarker {
                        ObjectIndex = index,
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
