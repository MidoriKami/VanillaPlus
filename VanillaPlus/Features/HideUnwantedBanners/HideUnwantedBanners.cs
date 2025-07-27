﻿using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Core;
using VanillaPlus.Extensions;

namespace VanillaPlus.HideUnwantedBanners;

public unsafe class HideUnwantedBanners : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Hide Unwanted Banners",
        Description = "Prevents large text banners from appearing and playing their sound effect.",
        Authors = ["MidoriKami"],
        Type = ModificationType.GameBehavior,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private delegate void ImageSetImageTextureDelegate(AtkUnitBase* addon, int bannerId, int a3, int sfxId);

    [Signature("48 89 5C 24 ?? 57 48 83 EC 30 48 8B D9 89 91", DetourName = nameof(OnSetImageTexture))]
    private readonly Hook<ImageSetImageTextureDelegate>? setImageTextureHook = null;

    private HideUnwantedBannersConfig config = null!;
    private HideUnwantedBannersConfigWindow configWindow = null!;

    public override bool HasConfigWindow => true;

    public override void OpenConfigWindow()
        => configWindow.Toggle();

    public override void OnEnable() {
        config = HideUnwantedBannersConfig.Load();
        configWindow = new HideUnwantedBannersConfigWindow(config);
        configWindow.AddToWindowSystem();
        
        Services.Hooker.InitializeFromAttributes(this);
        setImageTextureHook?.Enable();
    }

    public override void OnDisable() {
        configWindow.RemoveFromWindowSystem();
        setImageTextureHook?.Dispose();
    }

    private void OnSetImageTexture(AtkUnitBase* addon, int bannerId, int a3, int soundEffectId) {
        var skipOriginal = false;

        try {
            skipOriginal = config.HiddenBanners.Contains(bannerId);
        } catch (Exception e) {
            Services.PluginLog.Error(e, "Exception in OnSetImageTexture");
        }

        setImageTextureHook!.Original(addon, skipOriginal ? 0 : bannerId, a3, skipOriginal ? 0 : soundEffectId);
    }
}
