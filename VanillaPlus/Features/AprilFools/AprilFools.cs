using System.Collections.Generic;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;

namespace VanillaPlus.Features.AprilFools;

public class AprilFools : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "April Fools",
        Description = "\nDo you dare? Let's see who the real fool is.\n\nYou've been warned.",
        Type = ModificationType.Seasonal,
        Authors = [ "Evil MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "April Fools 2026"),
        ],
        CompatibilityModule = new AprilFoolsCompatabilityModule(),
    };

    public override string ImageName => "AprilFools.png";

    public override bool IsExperimental => true;

    private AprilFoolsConfig? config;
    private ConfigAddon? configAddon;

    private List<IFoolsModule>? modules;

    public override void OnEnable() {
        config = AprilFoolsConfig.Load();

        configAddon = new ConfigAddon {
            Title = "April Fools Config",
            InternalName = "AprilFoolsConfig",
            Config = config,
        };

        configAddon.AddCategory("Suffering Toggles")
            .AddCheckbox("Invert Scroll", nameof(config.InvertScroll))
            .AddCheckbox("Insane Scroll", nameof(config.InsaneScrollMode))
            .AddCheckbox("Indecisive Mode", nameof(config.Indecisive));

        OpenConfigAction = configAddon.Toggle;

        modules = [
            new ScrollingFools { Config = config },
            new IndecisiveFools {  Config = config },
        ];

        foreach (var module in modules) {
            module.Enable();
        }
    }

    public override void OnDisable() {
        foreach (var module in modules ?? []) {
            module.Disable();
        }
        modules?.Clear();
        modules = null;
        
        configAddon?.Dispose();
        configAddon = null;
        
        config = null;
    }
}
