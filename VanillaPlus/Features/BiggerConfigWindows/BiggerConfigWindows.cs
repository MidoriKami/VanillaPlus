using System.Threading.Tasks;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.BiggerConfigWindows;

public class BiggerConfigWindows : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.BiggerConfigWindows_DisplayName,
        Description = Strings.BiggerConfigWindows_Description,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "BiggerConfigWindows.png";

    private SystemConfigController? systemConfigController;
    private CharacterConfigController? characterConfigController;

    private BiggerConfigWindowsConfig? config;
    private ConfigAddon? configAddon;

    public override async Task OnEnableAsync() {
        config = await BiggerConfigWindowsConfig.Load();

        configAddon = new ConfigAddon {
            InternalName = "BiggerConfigWindowsConfig",
            Title = Strings.BiggerConfigWindows_ConfigTitle,
            Config = config,
        };

        configAddon.AddCategory(Strings.BiggerConfigWindows_CategoryGeneral)
            .AddInputFloat(Strings.BiggerConfigWindows_SystemConfigAdditionalSize, 5, ..4000, nameof(config.SystemConfigAdditionalHeight))
            .AddInputFloat(Strings.BiggerConfigWindows_CharacterConfigAdditionalSize, 5, ..4000, nameof(config.CharacterConfigAdditionalHeight));

        OpenConfigAction = configAddon.Toggle;

        systemConfigController = new SystemConfigController {
            Config = config,
        };

        await systemConfigController.EnableAsync();

        characterConfigController = new CharacterConfigController {
            Config = config,
        };

        await characterConfigController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        if (systemConfigController is not null) {
            await systemConfigController.DisableAsync();
            systemConfigController = null;
        }

        if (characterConfigController is not null) {
            await characterConfigController.DisableAsync();
            characterConfigController = null;
        }

        if (configAddon is not null) {
            await configAddon.DisposeAsync();
            configAddon = null;
        }

        config = null;
    }
}
