using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Overlay;
using KamiToolKit.Premade.Addons;
using KamiToolKit.Premade.SearchAddons;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.WindowBackground.Nodes;

namespace VanillaPlus.Features.WindowBackground;

public unsafe class WindowBackground : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_WindowBackground,
        Description = Strings.ModificationDescription_WindowBackground,
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Added search bar to search 'All Windows' in config"),
            new ChangeLogInfo(3, "Fixed incorrectly cleaning up removed backgrounds"),
            new ChangeLogInfo(4, "Rewrote module to be more stable"),
            new ChangeLogInfo(5, "Reimplemented system to allow configuring the color and size per window"),
        ],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@DutyListBackground"),
    };

    public override string ImageName => "WindowBackgrounds.png";

    private WindowBackgroundConfig? config;
    private ListConfigAddon<WindowBackgroundSetting, WindowBackgroundSettingListItemNode, WindowBackgroundConfigNode>? configWindow;
    private AddonSearchAddon? addonSearchAddon;

    private DynamicAddonController? dynamicAddonController;
    private OverlayController? overlayController;
    private List<WindowBackgroundImageNode>? backgroundImageNodes;

    public override void OnEnable() {
        backgroundImageNodes = [];

        config = WindowBackgroundConfig.Load();

        overlayController = new OverlayController();
        dynamicAddonController = new DynamicAddonController(config.Settings.Select(setting => setting.AddonName).ToArray());

        dynamicAddonController.OnAttach += AttachNode;
        dynamicAddonController.OnDetach += DetachNode;
        dynamicAddonController.OnUpdate += UpdateNode;

        dynamicAddonController.Enable();

        addonSearchAddon = new AddonSearchAddon {
            InternalName = "AddonSearch",
            Title = Strings.WindowBackground_SearchTitle,
            Size = new Vector2(350.0f, 600.0f),
        };

        configWindow = new ListConfigAddon<WindowBackgroundSetting, WindowBackgroundSettingListItemNode, WindowBackgroundConfigNode> {
            InternalName = "WindowBackgroundConfig",
            Title = Strings.WindowBackground_ConfigTitle,
            Size = new Vector2(600.0f, 500.0f),
            Options = config.Settings,

            // OnConfigChanged = _ => config.Save(),

            AddClicked = listNode => {
                addonSearchAddon.SelectionResult = searchResult => {
                    if (searchResult.Value is null)
                        return;

                    var newOption = new WindowBackgroundSetting {
                        AddonName = searchResult.Value->NameString,
                    };

                    config.Settings.Add(newOption);
                    config.Save();

                    dynamicAddonController.AddAddon(searchResult.Value->NameString);
                    listNode.RefreshList();
                };

                addonSearchAddon.Toggle();
            },

            RemoveClicked = (_, oldItem) => {
                config.Settings.Remove(oldItem);
                config.Save();
                dynamicAddonController.RemoveAddon(oldItem.AddonName);
            },
            ItemComparer = WindowBackgroundSetting.Compare,
            IsSearchMatch = WindowBackgroundSetting.IsMatch,
        };

        OpenConfigAction = configWindow.Toggle;
    }

    private void AttachNode(AtkUnitBase* addon) {
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

    private void UpdateNode(AtkUnitBase* addon) {
        if (config is null) return;
        if (backgroundImageNodes is null) return;

        var addonNodes = backgroundImageNodes
            .Where(node => !node.IsOverlayNode)
            .Where(node => node.Settings.AddonName == addon->NameString);

        foreach (var node in addonNodes) {
            node.Update();
        }
    }

    private void DetachNode(AtkUnitBase* addon) {
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

    private static AtkResNode* GetWindowNineGridNode(AtkComponentNode* windowNode) {
        foreach (var node in windowNode->Component->UldManager.Nodes) {
            if (node.Value is null) continue;
            if (node.Value->GetNodeType() is NodeType.NineGrid) {
                return node.Value;
            }
        }

        return null;
    }

    public override void OnDisable() {
        dynamicAddonController?.Dispose();
        dynamicAddonController = null;

        overlayController?.Dispose();
        overlayController = null;

        addonSearchAddon?.Dispose();
        addonSearchAddon = null;

        configWindow?.Dispose();
        configWindow = null;

        foreach (var node in backgroundImageNodes ?? []) {
            node.Dispose();
        }
        backgroundImageNodes?.Clear();
        backgroundImageNodes = null;

        config = null;
    }
}
