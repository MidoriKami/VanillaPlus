using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Components.Configuration;
using KamiToolKit.Components.Search;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.UiOverlay;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.WindowBackground.Nodes;

namespace VanillaPlus.Features.WindowBackground;

public class WindowBackground : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_WindowBackground,
        Description = Strings.ModificationDescription_WindowBackground,
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@DutyListBackground"),
    };

    public override string ImageName => "WindowBackgrounds.png";

    private WindowBackgroundConfig? config;
    private ConfigurationAddon<WindowBackgroundSetting, WindowBackgroundSettingListItemNode, WindowBackgroundConfigNode>? configAddon;
    private WindowSearchAddon? addonSearchAddon;

    private DynamicAddonController? dynamicAddonController;
    private OverlayController? overlayController;
    private List<WindowBackgroundImageNode>? backgroundImageNodes;

    public override async Task OnEnableAsync() {
        backgroundImageNodes = [];

        config = await WindowBackgroundConfig.Load();

        addonSearchAddon = new WindowSearchAddon {
            InternalName = "AddonSearch",
            Title = Strings.WindowBackground_SearchTitle,
            Size = new Vector2(350.0f, 600.0f),
            AllowMultiselect = true,
        };

        await Services.Framework.Run(() => {
            unsafe {
                overlayController = new OverlayController();

                dynamicAddonController = new DynamicAddonController {
                    AddonNames = config.Settings.Select(setting => setting.AddonName).ToList(),
                    OnSetup = AttachNode,
                    OnFinalize = DetachNode,
                    OnUpdate = UpdateNode,
                };

                dynamicAddonController.Enable();
            }
        });

        configAddon = new ConfigurationAddon<WindowBackgroundSetting, WindowBackgroundSettingListItemNode, WindowBackgroundConfigNode> {
            InternalName = "WindowBackgroundConfig",
            Title = Strings.WindowBackground_ConfigTitle,
            Size = new Vector2(600.0f, 500.0f),
            OptionsList = config.Settings,
            AddClicked = OnAddClicked,
            RemoveClicked = OnRemoveClicked,
            SaveConfig = () => Task.Run(config.Save),
            GetEntrySearchString = entry => entry.AddonName,
        };

        OpenConfigAction = configAddon.Toggle;
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            dynamicAddonController?.Dispose();
            overlayController?.Dispose();
        });

        dynamicAddonController = null;
        overlayController = null;

        await Task.WhenAll(
            addonSearchAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            configAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );

        addonSearchAddon = null;
        configAddon = null;

        backgroundImageNodes?.Clear();
        backgroundImageNodes = null;

        config = null;
    }

    private unsafe void AttachNode(AtkUnitBase* addon) {
        if (config is null) return;
        if (backgroundImageNodes is null) return;
        if (overlayController is null) return;

        var newNode = new WindowBackgroundImageNode {
            Position = -new Vector2(15.0f, 15.0f),
            Settings = config.GetSettings(addon->NameString),
        };

        if (addon->WindowNode is not null) {
            var targetNode = GetWindowNineGridNode(addon->WindowNode);
            if (targetNode is null) return;

            newNode.AttachNode(targetNode, NodePosition.BeforeTarget);
        }
        else {
            newNode.IsOverlayNode = true;
            overlayController.AddNode(newNode);
        }

        backgroundImageNodes.Add(newNode);
    }

    private unsafe void UpdateNode(AtkUnitBase* addon) {
        if (config is null) return;
        if (backgroundImageNodes is null) return;

        var addonNodes = backgroundImageNodes
            .Where(node => !node.IsOverlayNode)
            .Where(node => node.Settings.AddonName == addon->NameString);

        foreach (var node in addonNodes) {
            node.Update();
        }
    }

    private unsafe void DetachNode(AtkUnitBase* addon) {
        if (backgroundImageNodes is null) return;
        if (overlayController is null) return;

        foreach (var node in backgroundImageNodes.Where(node => node.Settings.AddonName == addon->NameString)) {
            if (node.IsOverlayNode) {
                overlayController.RemoveNode(node);
            }

            node.Dispose();
        }

        backgroundImageNodes.RemoveAll(node => node.Settings.AddonName == addon->NameString);
    }

    private unsafe void OnAddClicked() {
        if (config is null) return;
        if (dynamicAddonController is null) return;
        if (addonSearchAddon is null) return;

        addonSearchAddon.OptionsList = RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries
            .ToArray()
            .Where(option => option.Value is not null)
            .ToList();

        addonSearchAddon.ConfirmedSelections = selectionResults => {
            foreach (var result in selectionResults) {
                if (result.Value is null) continue;

                var newOption = new WindowBackgroundSetting {
                    AddonName = result.Value->NameString,
                };

                config.Settings.Add(newOption);
                dynamicAddonController.AddAddon(result.Value->NameString);
            }

            Task.Run(config.Save);
            configAddon?.OptionsList = config.Settings;
        };

        addonSearchAddon.Toggle();
    }

    private void OnRemoveClicked(WindowBackgroundSetting oldItem) {
        if (config is null) return;
        if (dynamicAddonController is null) return;

        config.Settings.Remove(oldItem);
        Task.Run(config.Save);

        dynamicAddonController.RemoveAddon(oldItem.AddonName);

        configAddon?.OptionsList = config.Settings;
    }

    private static unsafe AtkResNode* GetWindowNineGridNode(AtkComponentNode* windowNode) {
        foreach (var node in windowNode->Component->UldManager.Nodes) {
            if (node.Value is null) continue;
            if (node.Value->GetNodeType() is NodeType.NineGrid) {
                return node.Value;
            }
        }

        return null;
    }
}
