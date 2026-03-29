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

    private List<FoolsModule>? modules;

    public override void OnEnable() {
        config = AprilFoolsConfig.Load();
        
        modules = [
            new ScrollingFools { Config = config },
            new IndecisiveFools {  Config = config },
            new EmotionalDamageFools {  Config = config },
            new JustMonikaFools { Config = config },
            new DutyReadyFools { Config = config },
            new BetterCharacterPanelFools { Config = config },
            new FlippingOutFools { Config = config },
        ];

        configAddon = new ConfigAddon {
            Title = "April Fools Config",
            InternalName = "AprilFoolsConfig",
            Config = config,
        };

        configAddon.AddCategory("Suffering Toggles")
            .AddCheckbox("Invert Scroll", nameof(config.InvertScroll), 
                enabled => modules[0].Toggle(enabled))
            .AddTooltip("What? Can't deal with a little up'n down?")
            
            .AddCheckbox("Indecisive", nameof(config.Indecisive), 
                enabled => modules[1].Toggle(enabled))
            .AddTooltip("Having a hard time deciding? Don't worry, I'll give you some more options!")
            
            .AddCheckbox("Emotional Damage", nameof(config.EmotionalDamage), 
                enabled => modules[2].Toggle(enabled))
            .AddTooltip("It's self damage ya' know. Would be unfair to inflict onto others.")
            
            .AddCheckbox("Just Monika", nameof(config.JustMonika), 
                enabled => modules[3].Toggle(enabled))
            .AddTooltip("Just Monika.")
            
            .AddCheckbox("Duty Pop", nameof(config.DutyPop), 
                enabled => modules[4].Toggle(enabled))
            .AddTooltip("Queuing for lots of duties? They seem to be poppin a lot.")
            
            .AddCheckbox("Better Character Panel", nameof(config.BetterCharacterPanel), 
                enabled => modules[5].Toggle(enabled))
            .AddTooltip("Honestly, this one isn't even a prank, its just the way it should have been.")
            
            .AddCheckbox("Flipping Out", nameof(config.FlippingOut), 
                enabled => modules[6].Toggle(enabled))
            .AddTooltip("Placeholder text, make CERTAIN to replace this before releasing or else you'll look really silly. \n    - MidoriKami");

        OpenConfigAction = configAddon.Toggle;

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
