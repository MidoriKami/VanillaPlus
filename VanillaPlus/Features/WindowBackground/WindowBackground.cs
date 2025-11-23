using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.Controllers;
using KamiToolKit.Premade.Addons;
using KamiToolKit.Premade.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.WindowBackground;

public unsafe class WindowBackground : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Window Backgrounds",
        Description = "Allows you to add a background to any native window.\n\n" +
                      "Examples: Cast Bar, Target Health Bar, Inventory Widget, Todo List.",
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
    private ListConfigAddon<WindowBackgroundSetting, WindowBackgroundConfigNode>? configWindow;
    private SearchAddon<StringInfoNode>? addonSearchAddon;

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

        addonSearchAddon = new SearchAddon<StringInfoNode> {
            InternalName = "AddonSearch",
            Title = "Window Search",
            Size = new Vector2(350.0f, 600.0f),
            SortingOptions = [ "Visibility", "Alphabetical" ],
            SearchOptions = GetOptions(),
        };

        configWindow = new ListConfigAddon<WindowBackgroundSetting, WindowBackgroundConfigNode> {
            InternalName = "WindowBackgroundConfig",
            Title = "Window Backgrounds Config",
            Size = new Vector2(600.0f, 500.0f),
            Options = config.Settings,

            OnConfigChanged = _ => config.Save(),

            OnAddClicked = listNode => {
                addonSearchAddon.SearchOptions = GetOptions();
                addonSearchAddon.SelectionResult = searchResult => {
                    var newOption = new WindowBackgroundSetting {
                        AddonName = searchResult.Label,
                    };

                    listNode.AddOption(newOption);
                    dynamicAddonController.AddAddon(searchResult.Label);
                    config.Save();
                };

                addonSearchAddon.Toggle();
            },

            OnItemRemoved = oldItem => dynamicAddonController.RemoveAddon(oldItem.AddonName),
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
            overlayController.AddNode(newNode);
        }
        
        backgroundImageNodes.Add(newNode);
    }

    private void UpdateNode(AtkUnitBase* addon) {
        if (backgroundImageNodes is null) return;

        var nodesForAddon = backgroundImageNodes.Where(node => node.Settings.AddonName == addon->NameString);

        foreach (var node in nodesForAddon) {
            node.Update();
        }
    }

    private void DetachNode(AtkUnitBase* addon) {
        if (backgroundImageNodes is null) return;
        if (overlayController is null) return;

        var nodesForAddon = backgroundImageNodes.Where(node => node.Settings.AddonName == addon->NameString).ToList();

        foreach (var node in nodesForAddon) {
            if (addon->WindowNode is not null) {
                overlayController.RemoveNode(node);
            }

            backgroundImageNodes.Remove(node);
            node.Dispose();
        }
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

    private static List<StringInfoNode> GetOptions() {
        List<StringInfoNode> results = [];

        foreach (var unit in RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries) {
            if (unit.Value is null) continue;
            if (!unit.Value->IsReady) continue;
            
            results.Add(new AddonStringInfoNode {
                Label = unit.Value->NameString,
            });
        }

        return results;
    }
}
