using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Game.Config;
using Dalamud.Plugin.Services;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.GameConfigCommand;

public class GameConfigCommand : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_GameConfigCommand,
        Description = Strings.ModificationDescription_GameConfigCommand,
        Type = ModificationType.GameBehavior,
        Authors = ["Ren"],
    };

    private const string CommandName = "/gameconfig";
    private const string Usage = "Usage: /gameconfig [system|ui|control] <option> <value>";

    private static readonly ConfigOption[] ConfigOptions = [
        .. GetConfigOptions<SystemConfigOption>(ConfigSection.System),
        .. GetConfigOptions<UiConfigOption>(ConfigSection.UiConfig),
        .. GetConfigOptions<UiControlOption>(ConfigSection.UiControl),
    ];

    public override Task OnEnableAsync() {
        ICommandManager.Get().AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = Usage,
            ShowInHelp = true,
        });

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        ICommandManager.Get().RemoveHandler(CommandName);

        return Task.CompletedTask;
    }

    private static void OnCommand(string command, string arguments) {
        var remainingArguments = arguments.Trim();
        var optionName = TakeArgument(ref remainingArguments);
        ConfigSection? requestedSection = null;

        if (TryParseSection(optionName, out var section)) {
            requestedSection = section;
            optionName = TakeArgument(ref remainingArguments);
        }

        if (optionName.Length is 0 || remainingArguments.Length is 0) {
            IChatGui.Get().PrintError(Usage);
            return;
        }

        var matches = ConfigOptions
            .Where(option => requestedSection is null || option.Section == requestedSection)
            .Where(option => option.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase)
                || option.EnumName.Equals(optionName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count is 0) {
            IChatGui.Get().PrintError($"Unknown game configuration option: {optionName}");
            return;
        }

        if (matches.Count > 1) {
            var sections = string.Join(", ", matches.Select(option => GetSectionArgument(option.Section)));
            IChatGui.Get().PrintError($"{optionName} exists in multiple sections ({sections}). Specify a section.");
            return;
        }

        var configOption = matches[0];

        if (!configOption.Settable) {
            IChatGui.Get().PrintError($"{configOption.Name} cannot be changed while the game is running.");
            return;
        }

        if (ICondition.Get().IsInCombat) {
            IChatGui.Get().PrintError("Game configuration cannot be changed while in combat.");
            return;
        }

        try {
            SetValue(configOption, remainingArguments);
        }
        catch (Exception exception) {
            IPluginLog.Get().Error(exception, $"Failed to update {configOption.Section}.{configOption.Name}");
            IChatGui.Get().PrintError($"Failed to update {configOption.Name}.");
        }
    }

    private static void SetValue(ConfigOption option, string valueText) {
        var section = GetConfigSection(option.Section);

        switch (option.Type) {
            case ConfigType.UInt:
                if (!uint.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uintValue)) {
                    IChatGui.Get().PrintError($"{option.Name} requires an unsigned integer value.");
                    return;
                }

                if (section.TryGetProperties(option.Name, out UIntConfigProperties? uintProperties)
                    && uintProperties is not null
                    && (uintValue < uintProperties.Minimum || uintValue > uintProperties.Maximum)) {
                    IChatGui.Get().PrintError($"{option.Name} must be between {uintProperties.Minimum} and {uintProperties.Maximum}.");
                    return;
                }

                section.Set(option.Name, uintValue);
                break;

            case ConfigType.Float:
                if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue)
                    || !float.IsFinite(floatValue)) {
                    IChatGui.Get().PrintError($"{option.Name} requires a decimal value.");
                    return;
                }

                if (section.TryGetProperties(option.Name, out FloatConfigProperties? floatProperties)
                    && floatProperties is not null
                    && (floatValue < floatProperties.Minimum || floatValue > floatProperties.Maximum)) {
                    IChatGui.Get().PrintError($"{option.Name} must be between {floatProperties.Minimum} and {floatProperties.Maximum}.");
                    return;
                }

                section.Set(option.Name, floatValue);
                break;

            case ConfigType.String:
                section.Set(option.Name, valueText);
                break;

            default:
                IChatGui.Get().PrintError($"{option.Name} uses an unsupported configuration type.");
                return;
        }

        IChatGui.Get().Print($"Updated {GetSectionArgument(option.Section)}.{option.Name}.");
    }

    private static GameConfigSection GetConfigSection(ConfigSection section) => section switch {
        ConfigSection.System => IGameConfig.Get().System,
        ConfigSection.UiConfig => IGameConfig.Get().UiConfig,
        ConfigSection.UiControl => IGameConfig.Get().UiControl,
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
    };

    private static IEnumerable<ConfigOption> GetConfigOptions<TEnum>(ConfigSection section) where TEnum : struct, Enum {
        foreach (var option in Enum.GetValues<TEnum>()) {
            var enumName = option.ToString();
            var attribute = typeof(TEnum).GetField(enumName)?.GetCustomAttribute<GameConfigOptionAttribute>();

            if (attribute is not null) {
                yield return new ConfigOption(section, enumName, attribute.Name, attribute.Type, attribute.Settable);
            }
        }
    }

    private static string TakeArgument(ref string arguments) {
        arguments = arguments.TrimStart();

        var separatorIndex = arguments.IndexOf(' ');
        if (separatorIndex is -1) {
            var argument = arguments;
            arguments = string.Empty;
            return argument;
        }

        var result = arguments[..separatorIndex];
        arguments = arguments[(separatorIndex + 1)..].TrimStart();
        return result;
    }

    private static bool TryParseSection(string argument, out ConfigSection section) {
        switch (argument.ToLowerInvariant()) {
            case "system":
                section = ConfigSection.System;
                return true;
            case "ui":
                section = ConfigSection.UiConfig;
                return true;
            case "control":
                section = ConfigSection.UiControl;
                return true;
            default:
                section = default;
                return false;
        }
    }

    private static string GetSectionArgument(ConfigSection section) => section switch {
        ConfigSection.System => "system",
        ConfigSection.UiConfig => "ui",
        ConfigSection.UiControl => "control",
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
    };

    private enum ConfigSection {
        System,
        UiConfig,
        UiControl,
    }

    private sealed record ConfigOption(
        ConfigSection Section,
        string EnumName,
        string Name,
        ConfigType Type,
        bool Settable
    );
}
