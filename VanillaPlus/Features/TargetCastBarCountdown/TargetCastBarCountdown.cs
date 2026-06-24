using System.Threading.Tasks;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.TargetCastBarCountdown;

public class TargetCastBarCountdown : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_TargetCastBarCountdown,
        Description = Strings.ModificationDescription_TargetCastBarCountdown,
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@TargetCastbarCountdown"),
    };

    private PrimaryTargetCastbarController? primaryController;
    private PrimaryTargetAltCastbarController? primaryAltController;
    private FocusTargetCastbarController? focusController;
    private NameplateCastbarController? nameplateController;

    private TargetCastBarCountdownConfig? config;
    private ConfigAddon? configAddon;

    public override string ImageName => "TargetCastBarCountdown.png";

    public override async Task OnEnableAsync() {
        config = await TargetCastBarCountdownConfig.Load();

        primaryController = new PrimaryTargetCastbarController(config);
        primaryAltController = new PrimaryTargetAltCastbarController(config);
        focusController = new FocusTargetCastbarController(config);
        nameplateController = new NameplateCastbarController(config);

        configAddon = new ConfigAddon {
            InternalName = "TargetCastBarConfig",
            Title = Strings.TargetCastBarCountdown_ConfigTitle,
            Config = config,
        };

        configAddon.AddCategory(Strings.Toggles)
            .AddCheckbox(Strings.TargetCastBarCountdown_CheckboxPrimary, nameof(config.PrimaryTarget))
            .AddCheckbox(Strings.TargetCastBarCountdown_CheckboxFocus, nameof(config.FocusTarget));

        configAddon.AddCategory(Strings.TargetCastBarCountdown_CategoryPrimaryStyle)
            .AddNodeConfig(primaryController.LoadedStyle, TextNodeConfigOptions.TextAlignment);

        configAddon.AddCategory(Strings.TargetCastBarCountdown_CategoryPrimaryAltStyle)
            .AddNodeConfig(primaryAltController.LoadedStyle, TextNodeConfigOptions.TextAlignment);

        configAddon.AddCategory(Strings.TargetCastBarCountdown_CategoryFocusStyle)
            .AddNodeConfig(focusController.LoadedStyle, TextNodeConfigOptions.TextAlignment);

        configAddon.AddCategory(Strings.TargetCastBarCountdown_CategoryNameplateStyle)
            .AddNodeConfig(nameplateController.LoadedStyle, TextNodeConfigOptions.TextAlignment);

        OpenConfigAction = configAddon.Toggle;

        await Services.Framework.RunSafely(() => {
            primaryController.Enable();
            primaryAltController.Enable();
            focusController.Enable();
            nameplateController.Enable();
        });
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.RunSafely(() => {
            primaryController?.Dispose();
            primaryAltController?.Dispose();
            focusController?.Dispose();
            nameplateController?.Dispose();
        });

        primaryController = null;
        primaryAltController = null;
        focusController = null;
        nameplateController = null;

        await Task.WhenAll(configAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        configAddon = null;
    }
}
