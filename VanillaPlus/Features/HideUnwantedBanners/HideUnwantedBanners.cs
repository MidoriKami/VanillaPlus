using System;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.HideUnwantedBanners;

public unsafe class HideUnwantedBanners : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Hide Unwanted Banners",
        Description = "Prevents large text banners from appearing and playing their sound effect.",
        Authors = ["MidoriKami"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Configuration will now allow you to suppress new banners that you come across " +
                                 "once you see a new banner, it will appear in the configuration window."),
        ],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@HideUnwantedBanner"),
    };

    private Hook<AddonImage.Delegates.SetImage>? setImageTextureHook;

    private HideUnwantedBannersConfig? config;
    private NodeListAddon? configWindow;

    public override void OnEnable() {
        config = HideUnwantedBannersConfig.Load();

        configWindow = new NodeListAddon {
            InternalName = "BannersConfig",
            Title = "Hide Unwanted Banners Config",
            Size = new Vector2(500.0f, 600.0f),
            UpdateListFunction = UpdateList,
        };

        OpenConfigAction = configWindow.Toggle;

        setImageTextureHook = Services.Hooker.HookFromAddress<AddonImage.Delegates.SetImage>(AddonImage.Addresses.SetImage.Value, OnSetImageTexture);
        setImageTextureHook?.Enable();
    }

    public override void OnDisable() {
        configWindow?.Dispose();
        configWindow = null;
        
        setImageTextureHook?.Dispose();
        setImageTextureHook = null;

        config = null;
    }

    private bool UpdateList(VerticalListNode node, bool opening) {
        if (config is null) return false;

        foreach (var child in node.GetNodes<BannerInfoNode>()) {
            child.Update();
        }

        if (!opening) return false;

        foreach (var id in config.SeenBanners) {
            var newBannerInfoNode = new BannerInfoNode {
                Size = new Vector2(node.Width, 96.0f),
                ImageIconId = id,
                IsChecked = config.HiddenBanners.Contains(id),
                OnChecked = shouldHide => {
                    if (shouldHide) {
                        config.HiddenBanners.Add(id);
                    }
                    else {
                        config.HiddenBanners.Remove(id);
                    }

                    config.Save();
                },
            };

            node.AddNode(newBannerInfoNode);
        }

        return true;
    }

    private void OnSetImageTexture(AddonImage* addon, int bannerId, IconSubFolder language, int soundEffectId) {
        try {
            if (config is not null) {
                if (config.SeenBanners.Add((uint)bannerId)) {
                    config.Save();
                }

                if (config.HiddenBanners.Contains((uint)bannerId)) {
                    bannerId = 0;
                    soundEffectId = 0;
                }
            }
        } catch (Exception e) { 
            Services.PluginLog.Error(e, "Exception in OnSetImageTexture");
        }

        setImageTextureHook!.Original(addon, bannerId, language, soundEffectId);
    }
}
