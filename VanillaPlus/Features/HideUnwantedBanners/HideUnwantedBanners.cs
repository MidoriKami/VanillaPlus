using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.HideUnwantedBanners;

public class HideUnwantedBanners : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_HideUnwantedBanners,
        Description = Strings.ModificationDescription_HideUnwantedBanners,
        Authors = ["MidoriKami"],
        Type = ModificationType.GameBehavior,
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@HideUnwantedBanner"),
    };

    private Hook<AddonImage.Delegates.SetImage>? setImageTextureHook;

    private HideUnwantedBannersConfig? config;
    private NodeListAddon<BannerConfig, BannerConfigListItemNode>? configWindow;

    public override async Task OnEnableAsync() {
        config = await HideUnwantedBannersConfig.Load();

        configWindow = new NodeListAddon<BannerConfig, BannerConfigListItemNode> {
            InternalName = "BannersConfig",
            Title = Strings.HideUnwantedBanners_ConfigTitle,
            Size = new Vector2(550.0f, 650.0f),
            ListItems = config.BannerSettings,
            OnClose = () => Task.Run(config.Save),
        };

        OpenConfigAction = configWindow.Toggle;

        unsafe {
            setImageTextureHook = Services.Hooker.HookFromAddress<AddonImage.Delegates.SetImage>(AddonImage.Addresses.SetImage.Value, OnSetImageTexture);
            setImageTextureHook?.Enable();
        }
    }

    public override async Task OnDisableAsync() {
        await Task.WhenAll(configWindow?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        configWindow = null;

        setImageTextureHook?.Dispose();
        setImageTextureHook = null;

        config = null;
    }

    private unsafe void OnSetImageTexture(AddonImage* addon, int bannerId, IconSubFolder language, int soundEffectId) {
        try {
            if (config is not null) {
                if (config.BannerSettings.All(entry => entry.BannerId != bannerId)) {
                    config.BannerSettings.Add(new BannerConfig {
                        BannerId = bannerId,
                        IsSuppressed = false,
                    });
                    Task.Run(config.Save);
                }
                else {
                    if (config.BannerSettings.FirstOrDefault(entry => entry.BannerId == bannerId) is { IsSuppressed: true }) {
                        bannerId = 0;
                        soundEffectId = 0;
                    }
                }
            }
        }
        catch (Exception e) {
            Services.PluginLog.Exception(e);
        }

        setImageTextureHook!.Original(addon, bannerId, language, soundEffectId);
    }
}
