using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.BiggerConfigWindows;

public class CharacterConfigController : IDisposable {
    public required BiggerConfigWindowsConfig Config { get; init; }

    private AddonController? characterConfigController;
    private DynamicAddonController? childAddonController;

    // Doesn't seem like there's a way around this, the parent addon doesn't appear to have a reference to its children.
    private readonly List<string> childAddons = [
        "ConfigCharaOpeGeneral", "ConfigCharaOpeTarget", "ConfigCharaOpeCircle", "ConfigCharaOpeChara",
        "ConfigCharaItem", "ConfigCharacterHudGeneral", "ConfigCharacterHudHud", "ConfigCharaNamePlateOthers", "ConfigCharaNamePlateNpc",
        "ConfigCharaNamePlateGen", "ConfigCharaHotbarDisplay", "ConfigCharaHotbarXHB", "ConfigCharaHotbarXHBCustom", "ConfigCharaChatLogGen",
        "ConfigCharaChatLogDetail", "ConfigCharaChatLogRing",
    ];

    public unsafe void Enable() {
        characterConfigController = new AddonController {
            AddonName = "ConfigCharacter",
            OnSetup = SetupConfigCharacter,
            OnFinalize = FinalizeConfigCharacter,
        };

        childAddonController = new DynamicAddonController {
            AddonNames = childAddons,
            OnSetup = SetupConfigCharacterChild,
            OnFinalize = FinalizeConfigCharacterChild,
        };

        characterConfigController.Enable();
        childAddonController.Enable();
    }

    public void Dispose() {
        Services.Framework.Run(() => {
            characterConfigController?.Dispose();
            childAddonController?.Dispose();
        });

        characterConfigController = null;
        childAddonController = null;
    }

    private unsafe void SetupConfigCharacter(AtkUnitBase* addon) {
        if (AtkStage.Instance()->ScreenSize.Height < addon->Size.Y + Config.SystemConfigAdditionalHeight) {
            Services.ChatGui.PrintError("[BiggerConfigWindow] Unable to resize config window, height would be too big.", "VanillaPlus");
            Services.PluginLog.Warning("Unable to resize config window, height would be too big.", "BiggerConfigWindow");
            return;
        }

        addon->Size += new Vector2(0.0f, Config.CharacterConfigAdditionalHeight);

        // Adjust "Default" "Apply" "Close" container
        addon->GetNodeById(35)->Position += new Vector2(0.0f, Config.CharacterConfigAdditionalHeight);

        // Adjust vertical line ninegridnode
        addon->GetNodeById(34)->Size += new Vector2(0.0f, Config.CharacterConfigAdditionalHeight);
    }

    private unsafe void FinalizeConfigCharacter(AtkUnitBase* addon) {
        addon->Size -= new Vector2(0.0f, Config.CharacterConfigAdditionalHeight);

        // Adjust "Default" "Apply" "Close" container
        addon->GetNodeById(35)->Position -= new Vector2(0.0f, Config.CharacterConfigAdditionalHeight);

        // Adjust vertical line ninegridnode
        addon->GetNodeById(34)->Size -= new Vector2(0.0f, Config.CharacterConfigAdditionalHeight);
    }

    private unsafe void SetupConfigCharacterChild(AtkUnitBase* addon) {
        var scrollBarComponent = GetScrollbarForChild(addon);
        if (scrollBarComponent is null) return;

        ResizeHelpers.ResizeScrollBarNode(scrollBarComponent, Config.CharacterConfigAdditionalHeight);

        // Adjust list area stop, only visible in certain themes
        addon->GetNodeById(5)->Position += new Vector2(0.0f, Config.CharacterConfigAdditionalHeight);

        ApplyStupidFixes(addon);
    }

    private unsafe void FinalizeConfigCharacterChild(AtkUnitBase* addon) {
        var scrollBarComponent = GetScrollbarForChild(addon);
        if (scrollBarComponent is null) return;

        ResizeHelpers.ResizeScrollBarNode(scrollBarComponent, -Config.CharacterConfigAdditionalHeight);

        // Adjust list area stop, only visible in certain themes
        addon->GetNodeById(5)->Position -= new Vector2(0.0f, Config.CharacterConfigAdditionalHeight);

        ApplyStupidFixes(addon);
    }

    private unsafe AtkComponentScrollBar* GetScrollbarForChild(AtkUnitBase* addon)
        => (AtkComponentScrollBar*)Marshal.ReadIntPtr((nint)addon, sizeof(AtkUnitBase));

    /// <summary>
    /// Applies manual adjustments to certain nodes because square has
    /// to be extra and do shit in a weird way.
    /// </summary>
    private unsafe static void ApplyStupidFixes(AtkUnitBase* addon) {
        switch (addon->NameString) {
            case "ConfigCharaChatLogRing":
                var realContentsNode = addon->GetNodeById(5);
                if (realContentsNode is not null) {
                    realContentsNode->Position = Vector2.Zero;
                }
                return;
        }
    }
}
