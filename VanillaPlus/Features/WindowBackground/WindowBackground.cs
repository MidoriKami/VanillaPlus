using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
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
        ],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@DutyListBackground"),
    };

    public override string ImageName => "WindowBackgrounds.png";

    private OverlayAddonController? overlayAddonController;
    private Dictionary<string, AddonController>? addonControllers;

    private Dictionary<string, BackgroundImageNode>? backgroundImageNodes;
    private Dictionary<string, BackgroundImageNode>? overlayImageNodes;
    
    private WindowBackgroundConfig? config;
    private WindowBackgroundConfigWindow? configWindow;

    private SimpleOverlayNode? nameplateOverlayNode;

    private bool namePlateAddonReady;

    public override void OnEnable() {
        namePlateAddonReady = false;
        
        addonControllers = [];
        backgroundImageNodes = [];
        overlayImageNodes = [];

        config = WindowBackgroundConfig.Load();
        configWindow = new WindowBackgroundConfigWindow(config, OnStyleChanged, OnAddonRemoved, OnAddonAdded);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;

        overlayAddonController = new OverlayAddonController();

        overlayAddonController.OnAttach += addon => {
            var viewportSize = new Vector2(AtkStage.Instance()->ScreenSize.Width, AtkStage.Instance()->ScreenSize.Height);
            
            nameplateOverlayNode = new SimpleOverlayNode {
                Size = viewportSize,
                IsVisible = true,
            };
            
            System.NativeController.AttachNode(nameplateOverlayNode, addon->RootNode, NodePosition.AsFirstChild);
            namePlateAddonReady = true;

            foreach (var (_, controller) in addonControllers) {
                controller.Enable();
            }
        };

        overlayAddonController.OnUpdate += UpdateOverlayBackgrounds;

        overlayAddonController.OnDetach += _ => {
            foreach (var (_, controller) in addonControllers) {
                controller.Disable();
            }
            
            System.NativeController.DisposeNode(ref nameplateOverlayNode);
            namePlateAddonReady = false;
        };
        
        overlayAddonController.Enable();

        LoadAllBackgrounds();
    }

    public override void OnDisable() {
        UnloadAllBackgrounds();

        configWindow?.RemoveFromWindowSystem();
        configWindow = null;

        overlayAddonController?.Dispose();
        overlayAddonController = null;

        foreach (var (_, addonController) in addonControllers ?? []) {
            addonController.Dispose();
        }
        addonControllers?.Clear();
        addonControllers = null;

        backgroundImageNodes?.Clear();
        backgroundImageNodes = null;

        overlayImageNodes?.Clear();
        overlayImageNodes = null;

        config = null;
    }

    private void LoadAllBackgrounds() {
        if (config is null) return;

        foreach (var addonName in config.Addons) {
            OnAddonAdded(addonName);
        }
    }

    private void UnloadAllBackgrounds() {
        if (config is null) return;

        foreach (var addonName in config.Addons) {
            OnAddonRemoved(addonName);
        }
    }

    private void OnAddonAdded(string addonName) {
        if (addonControllers is null) return;

        if (addonControllers.ContainsKey(addonName)) return;

        var addonController = new AddonController(addonName);
        addonController.OnAttach += AttachNode;
        addonController.OnDetach += DetachNode;

        if (namePlateAddonReady) {
            addonController.Enable();
        }

        addonControllers.Add(addonName, addonController);
    }

    private void OnAddonRemoved(string addonName) {
        if (addonControllers?.TryGetValue(addonName, out var addonController) ?? false) {
            addonController.Disable();
            addonControllers?.Remove(addonName);
        }
    }

    private void AttachNode(AtkUnitBase* addon) {
        if (config is null) return;
        if (backgroundImageNodes is null) return;
        if (overlayImageNodes is null) return;
        
        // If we have a window node, attach before the first ninegrid node
        if (addon->WindowNode is not null) {
            if (!backgroundImageNodes.ContainsKey(addon->NameString)) {
                foreach (var node in addon->WindowNode->Component->UldManager.Nodes) {
                    if (node.Value is null) continue;
                    if (node.Value->GetNodeType() is NodeType.NineGrid) {
                    
                        var newBackgroundNode = new BackgroundImageNode {
                            Size = node.Value->Size() + config.Padding,
                            Position = -config.Padding / 2.0f,
                            Color = config.Color,
                            IsVisible = true,
                        };

                        System.NativeController.AttachNode(newBackgroundNode, node, NodePosition.BeforeTarget);
                        backgroundImageNodes.Add(addon->NameString, newBackgroundNode);
                        return;
                    }
                }
            }
        }

        // We don't have a window node, attach to nameplate
        else {
            if (!overlayImageNodes.ContainsKey(addon->NameString) && nameplateOverlayNode is not null) {
                var newBackgroundNode = new BackgroundImageNode {
                    Size = (addon->Size() + config.Padding) * addon->Scale,
                    Position = addon->Position() - config.Padding / 2.0f,
                    Color = config.Color,
                };

                System.NativeController.AttachNode(newBackgroundNode, nameplateOverlayNode);
                overlayImageNodes.Add(addon->NameString, newBackgroundNode);
            }
        }
    }

    private void DetachNode(AtkUnitBase* addon) {
        if (backgroundImageNodes is null) return;
        if (overlayImageNodes is null) return;
        
        if (addon->WindowNode is not null) {
            if (backgroundImageNodes.TryGetValue(addon->NameString, out var node)) {
                System.NativeController.DetachNode(node);
                node.Dispose();
                backgroundImageNodes.Remove(addon->NameString);
            }
        }
        else {
            if (overlayImageNodes.TryGetValue(addon->NameString, out var node)) {
                System.NativeController.DetachNode(node);
                node.Dispose();
                overlayImageNodes.Remove(addon->NameString);
            }
        }
    }

    private void UpdateOverlayBackgrounds(AddonNamePlate* _) {
        if (overlayImageNodes is null) return;
        if (config is null) return;

        foreach (var (name, imageNode) in overlayImageNodes) {
            var addon = Services.GameGui.GetAddonByName<AtkUnitBase>(name);
            imageNode.IsVisible = addon is not null && addon->IsActuallyVisible();
            
            if (addon is not null) {
                imageNode.Position = addon->Position() - config.Padding / 2.0f;
                imageNode.Size = (addon->Size() + config.Padding) * addon->Scale;
            }
        }
    }

    private void OnStyleChanged() {
        if (config is null) return;
        if (backgroundImageNodes is null) return;
        if (overlayImageNodes is null) return;

        foreach (var (addonName, imageNode) in backgroundImageNodes) {
            var addon = Services.GameGui.GetAddonByName<AtkUnitBase>(addonName);
            if (addon is not null) {
                imageNode.Color = config.Color;
                imageNode.Position = -config.Padding / 2.0f;
                imageNode.Size = addon->Size() + config.Padding;
            }
        }

        foreach (var (addonName, imageNode) in overlayImageNodes) {
            var addon = Services.GameGui.GetAddonByName<AtkUnitBase>(addonName);
            if (addon is not null) {
                imageNode.Color = config.Color;
                imageNode.Position = -config.Padding / 2.0f;
                imageNode.Size = (addon->Size() + config.Padding) * addon->Scale;
            }
        }
    }
}
