using System.Linq;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using KamiToolKit.MapOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ShowGatheringPoints;

public class ShowGatheringPoints : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ShowGatheringPoints,
        Description = Strings.ModificationDescription_ShowGatheringPoints,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = ["MidoriKami"],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    public override string ImageName => "ShowGatheringPoint.png";

    private MapOverlayController? mapOverlayController;

    public override async Task OnEnableAsync() {
        mapOverlayController = new MapOverlayController();

        await Services.GetService<IFramework>().RunSafely(() => {
            unsafe {
                foreach (var index in Enumerable.Range(0, EventObjectManager.Instance()->EventObjects.Length)) {
                    mapOverlayController.AddMarker(new GatheringPointMapMarker {
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
