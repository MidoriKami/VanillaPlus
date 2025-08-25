using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.HideUnwantedBanners;

public unsafe class HideUnwantedBanners : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Hide Unwanted Banners",
        Description = "Prevents large text banners from appearing and playing their sound effect.",
        Authors = ["MidoriKami"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@HideUnwantedBanner"),
    };

    private delegate void ImageSetImageTextureDelegate(AtkUnitBase* addon, int bannerId, int a3, int sfxId);

    [Signature("48 89 5C 24 ?? 57 48 83 EC 30 48 8B D9 89 91", DetourName = nameof(OnSetImageTexture))]
    private Hook<ImageSetImageTextureDelegate>? setImageTextureHook;

    private HideUnwantedBannersConfig? config;
    private HideUnwantedBannersConfigWindow? configWindow;

    public override void OnEnable() {
        config = HideUnwantedBannersConfig.Load();
        configWindow = new HideUnwantedBannersConfigWindow(config);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;
        
        Services.Hooker.InitializeFromAttributes(this);
        setImageTextureHook?.Enable();
    }

    public override void OnDisable() {
        configWindow?.RemoveFromWindowSystem();
        configWindow = null;
        
        setImageTextureHook?.Dispose();
        setImageTextureHook = null;
        
        config = null;
    }

    private void OnSetImageTexture(AtkUnitBase* addon, int bannerId, int a3, int soundEffectId) {
        var skipOriginal = false;

        try {
            if (config is null) {
                setImageTextureHook!.Original(addon, skipOriginal ? 0 : bannerId, a3, skipOriginal ? 0 : soundEffectId);
                return;
            }
            
            skipOriginal = config.HiddenBanners.Contains(bannerId);
        } catch (Exception e) {
            Services.PluginLog.Error(e, "Exception in OnSetImageTexture");
        }

        setImageTextureHook!.Original(addon, skipOriginal ? 0 : bannerId, a3, skipOriginal ? 0 : soundEffectId);
    }
}
