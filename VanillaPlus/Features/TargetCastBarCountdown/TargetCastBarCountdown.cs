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
        await primaryController.EnableAsync();

        primaryAltController = new PrimaryTargetAltCastbarController(config);
        await primaryAltController.EnableAsync();

        focusController = new FocusTargetCastbarController(config);
        await focusController.EnableAsync();

        nameplateController = new NameplateCastbarController();
        await nameplateController.EnableAsync();

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
    }

    public override async Task OnDisableAsync() {
        await primaryController.DisposeAsyncSafe();
        primaryController = null;

        await primaryAltController.DisposeAsyncSafe();
        primaryAltController = null;

        await focusController.DisposeAsyncSafe();
        focusController = null;

        await nameplateController.DisposeAsyncSafe();
        nameplateController = null;

        await configAddon.DisposeAsyncSafe();
        configAddon = null;
    }
}
