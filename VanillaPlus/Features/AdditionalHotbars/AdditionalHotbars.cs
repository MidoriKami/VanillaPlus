using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Components.Configuration;
using KamiToolKit.UiOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.AdditionalHotbars.Config;
using VanillaPlus.Features.AdditionalHotbars.Nodes;

namespace VanillaPlus.Features.AdditionalHotbars;

public class AdditionalHotbars : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Additional Hotbars",
        Description = "Allows you to add additional hotbars to the games UI.\n\n" +
                      "Warning, this is a highly experimental feature that is still a work in progress. " +
                      "Please submit feedback and issues to the XIVLauncher & Dalamud Discord thread for VanillaPlus.",
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "AdditionalHotbars.png";

    public override bool IsExperimental => true;

    private OverlayController? overlayController;
    private AdditionalHotbarsConfig? config;
    private ConfigurationAddon<HotbarConfig, HotbarListItemNode, HotbarSettingsNode>? configAddon;

    private List<HotbarOverlayNode>? nodes;

    public override async Task OnEnableAsync() {
        IGameInteropProvider.Get().InitializeFromAttributes(this);

        nodes = [];
        config = await AdditionalHotbarsConfig.Load();

        configAddon = new ConfigurationAddon<HotbarConfig, HotbarListItemNode, HotbarSettingsNode> {
            InternalName = "AdditionalHotbarsConfig",
            Title = "Additional Hotbars Config",
            SaveConfig = OnConfigChanged,
            GetEntrySearchString = entry => entry.Name,
            AddClicked = OnAddConfigClicked,
            RemoveClicked = OnRemoveConfigClicked,
            OptionsList = config.Hotbars,
            Size = new Vector2(750.0f, 550.0f),
        };

        OpenConfigAction = configAddon.Toggle;

        await IFramework.Get().Run(() => {
            overlayController = new OverlayController();

            unsafe {
                foreach (var hotbar in config.Hotbars) {
                    var newHotbarNode = new HotbarOverlayNode(config, hotbar) {
                        Position = hotbar.Position ?? (Vector2)AtkStage.Instance()->ScreenSize / 2.0f,
                    };

                    overlayController.AddNode(newHotbarNode);
                    nodes.Add(newHotbarNode);
                }
            }
        });

        ICommandManager.Get().AddHandler("/plushotbar", new CommandInfo(OnHotbarCommand) {
            AllowedInMacros = true,
            HelpMessage = "show, hide, or toggle additional hotbars.",
        });
    }

    public override async Task OnDisableAsync() {
        ICommandManager.Get().RemoveHandler("/plushotbar");

        await configAddon.DisposeAsyncSafe();
        configAddon = null;

        await IFramework.Get().Run( () => overlayController?.Dispose());
        overlayController = null;
        nodes = null;

        config = null;
    }

    private void OnConfigChanged() {
        if (config is null) return;

        config.Save();
        configAddon?.OptionsList = config.Hotbars;
    }

    private void OnAddConfigClicked() {
        if (config is null) return;

        var newHotbarConfig = new HotbarConfig {
            Name = "New Hotbar",
            Width = 12,
            Height = 1,
            Slots = List<SlotData>.CreateInitialized(12),
        };

        unsafe {
            var newHotbarNode = new HotbarOverlayNode(config, newHotbarConfig) {
                Position = (Vector2)AtkStage.Instance()->ScreenSize / 2.0f,
            };

            overlayController?.AddNode(newHotbarNode);
            nodes?.Add(newHotbarNode);
        }

        config.Hotbars.Add(newHotbarConfig);
        config.Save();

        configAddon?.OptionsList = config.Hotbars;
    }

    private void OnRemoveConfigClicked(HotbarConfig removedHotbar) {
        if (config is null) return;

        if (nodes?.FirstOrDefault(node => node.Config == removedHotbar) is {} nodeToRemove) {
            overlayController?.RemoveNode(nodeToRemove);
            nodes?.Remove(nodeToRemove);
        }

        config.Save();
        configAddon?.OptionsList = config.Hotbars;
    }

    private void OnHotbarCommand(string command, string arguments) {
        if (command is not "/plushotbar") return;

        if (arguments.Split(" ", 2) is not [var action, var hotbarName]) return;
        if (hotbarName.IsNullOrEmpty()) return;

        var configEntry = config?.Hotbars.FirstOrDefault(bar => bar.Name.Equals(hotbarName, StringComparison.OrdinalIgnoreCase));

        if (configEntry is null) {
            IChatGui.Get().PrintError($"Unable to find a hotbar by the name of '{hotbarName}'");
            return;
        }

        switch (action.ToLower()) {
            case "show":
                configEntry.IsEnabled = true;
                break;

            case "hide":
                configEntry.IsEnabled = false;
                break;

            case "toggle":
                configEntry.IsEnabled = !configEntry.IsEnabled;
                break;
        }

        config?.Save();
    }
}
