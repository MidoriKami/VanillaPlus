using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.BiggerConfigWindows;

public class BiggerConfigWindows : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Bigger Config Windows",
        Description = "Increases the vertical height of the games character and system config windows.",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
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
            Title = "Bigger Config Windows Config",
            Config = config,
        };

        configAddon.AddCategory("General")
            .AddInputFloat("System Config Additional Size", 5, ..4000, nameof(config.SystemConfigAdditionalHeight))
            .AddInputFloat("Character Config Additional Size", 5, ..4000, nameof(config.CharacterConfigAdditionalHeight));

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
