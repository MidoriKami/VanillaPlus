using System;
using System.Linq;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.HideUnwantedBanners;

public unsafe class HideUnwantedBanners : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_HideUnwantedBanners,
        Description = Strings.ModificationDescription_HideUnwantedBanners,
        Authors = ["MidoriKami"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Configuration will now allow you to suppress new banners that you come across " +
                                 "once you see a new banner, it will appear in the configuration window."),
            new ChangeLogInfo(3, "Rebuilt feature to utilize new systems, however config system had to be reimplemented, " +
                                 "configs have been reset."),
        ],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@HideUnwantedBanner"),
    };

    private Hook<AddonImage.Delegates.SetImage>? setImageTextureHook;

    private HideUnwantedBannersConfig? config;
    private NodeListAddon<BannerConfig, BannerConfigListItemNode>? configWindow;

    public override void OnEnable() {
        config = HideUnwantedBannersConfig.Load();

        configWindow = new NodeListAddon<BannerConfig, BannerConfigListItemNode> {
            InternalName = "BannersConfig",
            Title = Strings.HideUnwantedBanners_ConfigTitle,
            Size = new Vector2(550.0f, 650.0f),
            ListItems = config.BannerSettings,
            OnClose = config.Save,
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

    private void OnSetImageTexture(AddonImage* addon, int bannerId, IconSubFolder language, int soundEffectId) {
        try {
            if (config is not null) {
                if (config.BannerSettings.All(entry => entry.BannerId != bannerId)) {
                    config.BannerSettings.Add(new BannerConfig {
                        BannerId = bannerId,
                        IsSuppressed = false,
                    });
                    config.Save();
                }
                else {
                    if (config.BannerSettings.FirstOrDefault(entry => entry.BannerId == bannerId) is { IsSuppressed: true }) {
                        bannerId = 0;
                        soundEffectId = 0;
                    }
                }
            }
        } catch (Exception e) { 
            Services.PluginLog.Error(e, "Exception in OnSetImageTexture");
        }

        setImageTextureHook!.Original(addon, bannerId, language, soundEffectId);
    }
}
