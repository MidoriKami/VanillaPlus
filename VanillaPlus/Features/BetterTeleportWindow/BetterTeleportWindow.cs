using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.BetterTeleportWindow;

public class BetterTeleportWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_BetterTeleportWindow,
        Description = Strings.ModificationDescription_BetterTeleportWindow,
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
    };

    public override string ImageName => "BetterTeleportWindow.png";

    internal static BetterTeleportWindowConfig? Config;
    internal static TeleportAddon? CustomTeleportAddon;

    private AddonFactoryController? teleportFactoryController;

    public override async Task OnEnableAsync() {
        Config = await BetterTeleportWindowConfig.Load();

        teleportFactoryController = new AddonFactoryController {
            AddonName = "Teleport",
            CreateNativeAddonFunction = () => CustomTeleportAddon = new TeleportAddon(Config) {
                InternalName = "Teleport",
                Title = Strings.BetterTeleportWindow_WindowTitle,
                Size = new Vector2(700.0f, 600.0f),
            },
        };

        await Service<IFramework>.Get().RunSafely(teleportFactoryController.Enable);
    }

    public override async Task OnDisableAsync() {
        await Service<IFramework>.Get().RunSafely(() => teleportFactoryController?.Dispose());
        teleportFactoryController = null;
    }
}
