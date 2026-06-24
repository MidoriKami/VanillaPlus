using System.Numerics;
using System.Threading.Tasks;
using KamiToolKit.UiOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.ZoneTransitionLabels.Nodes;

namespace VanillaPlus.Features.ZoneTransitionLabels;

public class ZoneTransitionLabels : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ZoneTransitionLabels,
        Description = Strings.ModificationDescription_ZoneTransitionLabels,
        Type = ModificationType.NewOverlay,
        Authors = ["Abyeon"],
    };

    public override string ImageName => "ZoneTransitionLabels/ZoneTransitionLabels.png";

    // public override bool IsExperimental => true;

    private OverlayController? overlayController;
    private ZoneWatcher? zoneWatcher;

    public override async Task OnEnableAsync() {
        zoneWatcher = new ZoneWatcher();

        await Services.Framework.RunSafely(() => {
            overlayController = new OverlayController();
            overlayController.AddNode(new ZoneLabelNode(zoneWatcher) {
                Size = new Vector2(30, 30)
            });
        });
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.RunSafely(() => {
            overlayController?.Dispose();
            zoneWatcher?.Dispose();
        });

        overlayController = null;
        zoneWatcher = null;
    }
}
