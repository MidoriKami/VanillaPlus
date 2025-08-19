using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Extensions;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

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
        ],
        CompatabilityModule = new SimpleTweaksCompatabilityModule("UiAdjustments@DutyListBackground"),
    };

    private List<AddonBackground>? addonBackgrounds;
    private WindowBackgroundConfig? config;
    private WindowBackgroundConfigWindow? configWindow;

    public override string ImageName => "WindowBackgrounds.png";

    public override bool IsExperimental => true;

    public override void OnEnable() {
        addonBackgrounds = [];
        
        config = WindowBackgroundConfig.Load();
        configWindow = new WindowBackgroundConfigWindow(config, UpdateListeners, OnStyleChanged);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;
        
        UpdateListeners();
    }

    public override void OnDisable() {
        if (configWindow is null) return;
        
        configWindow.RemoveFromWindowSystem();
        configWindow = null;
        
        Services.AddonLifecycle.UnregisterListener(OnAddonSetup, OnAddonFinalize, OnAddonUpdate);

        foreach (var background in addonBackgrounds ?? []) {
            System.NativeController.DetachNode(background.ImageNode, () => {
                background.ImageNode.Dispose();
            });
        }
        addonBackgrounds?.Clear();
        addonBackgrounds = null;

        config = null;
    }
    
    private void UpdateListeners() {
        if (config is null) return;
        
        Services.AddonLifecycle.UnregisterListener(OnAddonSetup, OnAddonFinalize, OnAddonUpdate);
        if (config.Addons.Count == 0) return;

        Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, config.Addons, OnAddonSetup);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, config.Addons, OnAddonFinalize);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, config.Addons, OnAddonUpdate);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, config.Addons, OnAddonUpdate);

        foreach (var addonName in config.Addons) {
            var addonPointer = Services.GameGui.GetAddonByName<AtkUnitBase>(addonName);
            if (addonPointer is not null) {
                AttachNode(addonPointer);
            }
        }

        var orphanedBackgrounds = addonBackgrounds?.Where(background => !config.Addons.Any(option => option == background.AddonName)).ToList() ?? [];
        foreach (var background in orphanedBackgrounds) {
            System.NativeController.DetachNode(background.ImageNode, () => {
                background.ImageNode.Dispose();
            });
            addonBackgrounds?.Remove(background);
        }
    }
    
    private void OnAddonSetup(AddonEvent type, AddonArgs args)
        => AttachNode(args.GetAddon<AtkUnitBase>());
    
    private void OnAddonUpdate(AddonEvent type, AddonArgs args) {
        if (config is null) return;
        
        if (addonBackgrounds?.FirstOrDefault(background => background.AddonName == args.AddonName) is { } info) {
            info.ImageNode.Size = args.GetAddon<AtkUnitBase>()->Size() + config.Padding;
        }
    }
    
    private void OnAddonFinalize(AddonEvent type, AddonArgs args) {
        if (addonBackgrounds?.FirstOrDefault(background => background.AddonName == args.AddonName) is { } info) {
            System.NativeController.DetachNode(info.ImageNode, () => {
                info.ImageNode.Dispose();
            });

            addonBackgrounds.Remove(info);
        }
    }

    private void AttachNode(AtkUnitBase* addon) {
        if (config is null) return;
        
        if (!addonBackgrounds?.Any(background => background.AddonName == addon->NameString) ?? false) return; {
            var newBackgroundNode = new BackgroundImageNode {
                Size = addon->Size() + config.Padding,
                Position = -config.Padding / 2.0f,
                Color = config.Color,
                IsVisible = true,
            };

            if (addon->RootNode->ChildNode is not null) {
                System.NativeController.AttachNode(newBackgroundNode, addon->RootNode->ChildNode, NodePosition.BeforeAllSiblings);
                addonBackgrounds?.Add(new AddonBackground(addon->NameString, newBackgroundNode));
            }
        }
    }
    
    private void OnStyleChanged() {
        if (config is null) return;
        
        foreach (var background in addonBackgrounds ?? []) {
            var addon = Services.GameGui.GetAddonByName<AtkUnitBase>(background.AddonName);
            if (addon is not null) {
                background.ImageNode.Color = config.Color;
                background.ImageNode.Position = -config.Padding / 2.0f;
                background.ImageNode.Size = addon->Size() + config.Padding;
            }
        }
    }
}
