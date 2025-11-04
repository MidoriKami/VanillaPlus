using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.WindowBackground;

public unsafe class WindowBackgroundController : IDisposable {
    private OverlayAddonController? overlayAddonController;
    private SimpleOverlayNode? nameplateOverlayNode;

    private ConcurrentDictionary<string, AddonController>? addonControllers;

    private Dictionary<string, BackgroundImageNode>? backgroundImageNodes;
    private Dictionary<string, BackgroundImageNode>? overlayImageNodes;

    private bool namePlateAddonReady;

    private readonly WindowBackgroundConfig config;

    public WindowBackgroundController(WindowBackgroundConfig config) {
        this.config = config;
        namePlateAddonReady = false;

        addonControllers = [];
        backgroundImageNodes = [];
        overlayImageNodes = [];
        
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
    
    public void Dispose() {
        UnloadAllBackgrounds();
        
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
    }

    public void AddAddon(string addonName) {
        if (addonName == string.Empty) return;
        if (addonName == WindowBackgroundSetting.InvalidName) return;
        if (addonControllers is null) return;

        if (addonControllers.ContainsKey(addonName)) return;

        var addonController = new AddonController(addonName);
        addonController.OnAttach += AttachNode;
        addonController.OnDetach += DetachNode;

        if (namePlateAddonReady) {
            addonController.Enable();
        }

        addonControllers.TryAdd(addonName, addonController);

        config.Save();
    }

    public void RemoveAddon(string addonName) {
        if (addonName == string.Empty) return;
        if (addonName == WindowBackgroundSetting.InvalidName) return;
        if (addonControllers?.TryGetValue(addonName, out var addonController) ?? false) {
            addonController.Disable();
            addonControllers?.Remove(addonName, out _);
        }

        config.Save();
    }

    private void AttachNode(AtkUnitBase* addon) {
        if (backgroundImageNodes is null) return;
        if (overlayImageNodes is null) return;
        
        // If we have a window node, attach before the first ninegrid node
        if (addon->WindowNode is not null) {
            if (!backgroundImageNodes.ContainsKey(addon->NameString)) {
                foreach (var node in addon->WindowNode->Component->UldManager.Nodes) {
                    if (node.Value is null) continue;
                    if (node.Value->GetNodeType() is NodeType.NineGrid) {

                        var windowConfig = config.GetSettings(addon->NameString);

                        var newBackgroundNode = new BackgroundImageNode {
                            Size = node.Value->Size() + windowConfig.Padding,
                            Position = -windowConfig.Padding / 2.0f,
                            Color = windowConfig.Color,
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

                var windowConfig = config.GetSettings(addon->NameString);

                var newBackgroundNode = new BackgroundImageNode {
                    Size = (addon->RootSize() + windowConfig.Padding) * addon->Scale,
                    Position = addon->Position() - windowConfig.Padding / 2.0f,
                    Color = windowConfig.Color,
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

        foreach (var (name, imageNode) in overlayImageNodes) {
            var addon = Services.GameGui.GetAddonByName<AtkUnitBase>(name);
            imageNode.IsVisible = addon is not null && addon->IsActuallyVisible();

            var windowConfig = config.GetSettings(addon->NameString);

            if (addon is not null) {
                imageNode.Color = windowConfig.Color;
                imageNode.Position = addon->Position() - windowConfig.Padding / 2.0f;
                imageNode.Size = (addon->RootSize() + windowConfig.Padding) * addon->Scale;
            }
        }
    }
    
    private void LoadAllBackgrounds() {
        foreach (var setting in config.Settings) {
            AddAddon(setting.AddonName);
        }
    }

    private void UnloadAllBackgrounds() {
        foreach (var setting in config.Settings) {
            RemoveAddon(setting.AddonName);
        }
    }

    public void UpdateNodeStyles() {
        if (backgroundImageNodes is null) return;
        if (overlayImageNodes is null) return;

        foreach (var (addonName, imageNode) in backgroundImageNodes) {
            var addon = Services.GameGui.GetAddonByName<AtkUnitBase>(addonName);

            var windowConfig = config.GetSettings(addon->NameString);

            if (addon is not null) {
                imageNode.Color = windowConfig.Color;
                imageNode.Position = -windowConfig.Padding / 2.0f;
                imageNode.Size = addon->RootSize() + windowConfig.Padding;
            }
        }
    }
}
