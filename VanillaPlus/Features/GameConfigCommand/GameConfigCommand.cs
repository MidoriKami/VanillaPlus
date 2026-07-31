using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Game.Config;
using Dalamud.Plugin.Services;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.GameConfigCommand;

public class GameConfigCommand : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_GameConfigCommand,
        Description = Strings.ModificationDescription_GameConfigCommand,
        Type = ModificationType.GameBehavior,
        Authors = [ "Ren" ],
    };

    private static Dictionary<ConfigSection, Dictionary<string, GameConfigOptionAttribute>>? configMapping;

    public override Task OnEnableAsync() {
        ICommandManager.Get().AddHandler("/gameconfig", new CommandInfo(OnCommand) {
            HelpMessage = "Usage: /gameconfig <system|ui|control> <option> <value>",
            ShowInHelp = true,
        });

        configMapping = new Dictionary<ConfigSection, Dictionary<string, GameConfigOptionAttribute>> {
            [ConfigSection.System] =
                Enum.GetValues<SystemConfigOption>()
                    .Select(option => option.GetAttribute<GameConfigOptionAttribute>())
                    .OfType<GameConfigOptionAttribute>()
                    .ToDictionary(key => key.Name.ToLowerInvariant(), value => value),

            [ConfigSection.UiConfig] =
                Enum.GetValues<UiConfigOption>()
                    .Select(option => option.GetAttribute<GameConfigOptionAttribute>())
                    .OfType<GameConfigOptionAttribute>()
                    .ToDictionary(key => key.Name.ToLowerInvariant(), value => value),

            [ConfigSection.UiControl] =
                Enum.GetValues<UiControlOption>()
                    .Select(option => option.GetAttribute<GameConfigOptionAttribute>())
                    .OfType<GameConfigOptionAttribute>()
                    .ToDictionary(key => key.Name.ToLowerInvariant(), value => value),
        };

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        ICommandManager.Get().RemoveHandler("/gameconfig");

        configMapping = null;

        return Task.CompletedTask;
    }

    private static void OnCommand(string command, string arguments) {
        if (command is not "/gameconfig") return;

        if (ICondition.Get().IsInCombat) {
            IChatGui.Get().PrintError("Game configuration cannot be changed while in combat.", "VanillaPlus");
            return;
        }

        switch (arguments.ToLowerInvariant().Split(' ')) {
            case ["system", { Length: > 0 } option, { Length: > 0 } value]: {
                if (configMapping?[ConfigSection.System].TryGetValue(option.ToLowerInvariant(), out var optionAttribute) ?? false) {
                    SetConfigOption(ConfigSection.System, optionAttribute, value);
                }
                break;
            }

            case ["ui", { Length: > 0 } option, { Length: > 0 } value]: {
                if (configMapping?[ConfigSection.UiConfig].TryGetValue(option.ToLowerInvariant(), out var optionAttribute) ?? false) {
                    SetConfigOption(ConfigSection.UiConfig, optionAttribute, value);
                }
                break;
            }

            case ["control", { Length: > 0 } option, { Length: > 0 } value]: {
                if (configMapping?[ConfigSection.UiControl].TryGetValue(option.ToLowerInvariant(), out var optionAttribute) ?? false) {
                    SetConfigOption(ConfigSection.UiControl, optionAttribute, value);
                }
                break;
            }

            default:
                IChatGui.Get().PrintError(
                    $"Error processing command for {command} {arguments}, check the arguments and try again.\n" +
                    $"Usage: /gameconfig <system|ui|control> <option> <value>", "VanillaPlus"
                );
                break;
        }
    }

    private static void SetConfigOption(ConfigSection section, GameConfigOptionAttribute option, string valueString) {
        if (option is { Settable: false }) {
            IChatGui.Get().PrintError($"Config option {option.Name} is not allowed to be set.", "VanillaPlus");
            return;
        }

        var configSection = section switch {
            ConfigSection.System => IGameConfig.Get().System,
            ConfigSection.UiConfig => IGameConfig.Get().UiConfig,
            ConfigSection.UiControl => IGameConfig.Get().UiControl,
            _ => null,
        };

        if (configSection is null) return;

        try {
            switch (option) {
                case { Type: ConfigType.UInt, Name: var name } when uint.TryParse(valueString, out var uintValue):

                    if (configSection.TryGetProperties(name, out UIntConfigProperties? uintProperties) && uintProperties is not null) {
                        if (uintValue < uintProperties.Minimum || uintValue > uintProperties.Maximum) {
                            IChatGui.Get().PrintError($"Argument for {option.Name} must be between {uintProperties.Minimum} and {uintProperties.Maximum}.", "VanillaPlus");
                            return;
                        }
                    }

                    configSection.Set(name, uintValue);
                    IChatGui.Get().Print($"Updated {section.Description}.{option.Name}.");
                    return;

                case { Type: ConfigType.Float, Name: var name } when float.TryParse(valueString, out var floatValue):

                    if (!float.IsFinite(floatValue)) {
                        IChatGui.Get().PrintError($"{option.Name} requires a decimal value.", "VanillaPlus");
                        return;
                    }

                    if (configSection.TryGetProperties(name, out FloatConfigProperties? floatProperties) && floatProperties is not null) {
                        if (floatValue < floatProperties.Minimum || floatValue > floatProperties.Maximum) {
                            IChatGui.Get().PrintError($"{option.Name} must be between {floatProperties.Minimum} and {floatProperties.Maximum}.", "VanillaPlus");
                            return;
                        }
                    }

                    configSection.Set(name, floatValue);
                    IChatGui.Get().Print($"Updated {section.Description}.{option.Name}.");
                    return;

                case { Type: ConfigType.String, Name: var name }:
                    configSection.Set(name, valueString);
                    IChatGui.Get().Print($"Updated {section.Description}.{option.Name}.");
                    return;

                default:
                    IChatGui.Get().PrintError($"Error processing command for {option.Name}, check the arguments and try again.", "VanillaPlus");
                    return;
            }
        }
        catch (Exception e) {
            IChatGui.Get().PrintError($"Error processing command for {option.Name}, check the arguments and try again.", "VanillaPlus");
            IPluginLog.Get().Exception(e);
        }
    }
}
