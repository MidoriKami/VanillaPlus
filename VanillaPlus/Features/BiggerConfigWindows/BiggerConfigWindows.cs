using System.Threading.Tasks;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;

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

        characterConfigController = new CharacterConfigController {
            Config = config,
        };

        await Services.Framework.Run(() => {
            systemConfigController.Enable();
            characterConfigController.Enable();
        });
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            systemConfigController?.Dispose();
            characterConfigController?.Dispose();
        });

        systemConfigController = null;
        characterConfigController = null;

        await Task.WhenAll(configAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        configAddon = null;

        config = null;
    }
}
