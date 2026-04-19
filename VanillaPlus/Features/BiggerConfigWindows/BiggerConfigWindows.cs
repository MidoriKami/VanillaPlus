using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.BiggerConfigWindows;

public class BiggerConfigWindows : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.BiggerConfigWindows_DisplayName,
        Description = Strings.BiggerConfigWindows_Description,
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
    };

    public override string ImageName => "BiggerConfigWindows.png";

    private SystemConfigController? systemConfigController;
    private CharacterConfigController? characterConfigController;

    private BiggerConfigWindowsConfig? config;
    private ConfigAddon? configAddon;

    public override void OnEnable() {
        config = BiggerConfigWindowsConfig.Load();

        configAddon = new ConfigAddon {
            InternalName = "BiggerConfigWindowsConfig",
            Title = Strings.BiggerConfigWindows_ConfigTitle,
            Config = config,
        };

        configAddon.AddCategory(Strings.BiggerConfigWindows_CategoryGeneral)
            .AddInputFloat(Strings.BiggerConfigWindows_SystemConfigAdditionalSize, 5, ..4000, nameof(config.SystemConfigAdditionalHeight))
            .AddInputFloat(Strings.BiggerConfigWindows_CharacterConfigAdditionalSize, 5, ..4000, nameof(config.CharacterConfigAdditionalHeight));

        OpenConfigAction = configAddon.Toggle;
        
        systemConfigController = new SystemConfigController(config);
        characterConfigController = new CharacterConfigController(config);
    }

    public override void OnDisable() {
        systemConfigController?.Dispose();
        systemConfigController = null;
        
        characterConfigController?.Dispose();
        characterConfigController = null;
        
        configAddon?.Dispose();
        configAddon = null;
        
        config = null;
    }
}
