using System.Numerics;
using System.Threading.Tasks;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.BetterTeleportWindow;

public class BetterTeleportWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Better Teleport Window",
        Description = "Replaces the games Teleport window with a better custom made version.\n\n" +
                      "Note: Configuration such as using Teleport Tickets must be done from the original teleport window.",
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
    };

    public override string ImageName => "BetterTeleportWindow.png";

    // Hyper Experimental lol. Game go boom, probably.
    public override bool IsExperimental => true;

    internal static BetterTeleportWindowConfig? Config;
    internal static TeleportAddon? CustomTeleportAddon;

    private AddonFactoryController? teleportFactoryController;

    public override async Task OnEnableAsync() {
        Config = await BetterTeleportWindowConfig.Load();

        teleportFactoryController = new AddonFactoryController {
            AddonName = "Teleport",
            CreateNativeAddonFunction = () => CustomTeleportAddon = new TeleportAddon(Config) {
                InternalName = "Teleport",
                Title = "Teleport",
                Size = new Vector2(700.0f, 600.0f),
            },
        };
        teleportFactoryController.Enable();
    }

    public override Task OnDisableAsync() {
        teleportFactoryController?.Dispose();
        teleportFactoryController = null;

        return Task.CompletedTask;
    }
}
