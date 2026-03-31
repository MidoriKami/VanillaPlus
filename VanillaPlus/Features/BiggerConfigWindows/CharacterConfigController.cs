using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.BiggerConfigWindows;

public unsafe class CharacterConfigController : IDisposable {
    private readonly BiggerConfigWindowsConfig config;

    private readonly AddonController characterConfigController;
    private readonly DynamicAddonController childAddonController;

    // Doesn't seem like there's a way around this, the parent addon doesn't appear to have a reference to its children.
    private readonly List<string> childAddons = [ "ConfigCharaOpeGeneral", "ConfigCharaOpeTarget", "ConfigCharaOpeCircle", "ConfigCharaOpeChara", 
        "ConfigCharaItem", "ConfigCharacterHudGeneral", "ConfigCharacterHudHud", "ConfigCharaNamePlateOthers", "ConfigCharaNamePlateNpc", 
        "ConfigCharaNamePlateGen", "ConfigCharaHotbarDisplay", "ConfigCharaHotbarXHB", "ConfigCharaHotbarXHBCustom", "ConfigCharaChatLogGen", 
        "ConfigCharaChatLogDetail", "ConfigCharaChatLogRing",
    ];
    
    public CharacterConfigController(BiggerConfigWindowsConfig config) {
        this.config = config;
        characterConfigController = new AddonController {
            AddonName = "ConfigCharacter",
            OnSetup = SetupConfigCharacter,
            OnFinalize = FinalizeConfigCharacter,
        };
        characterConfigController.Enable();

        childAddonController = new DynamicAddonController {
            AddonNames = childAddons,
            OnSetup = SetupConfigCharacterChild,
            OnFinalize = FinalizeConfigCharacterChild,
        };
        childAddonController.Enable();
    }

    public void Dispose() {
        childAddonController.Dispose();
        characterConfigController.Dispose();
    }

    private void SetupConfigCharacter(AtkUnitBase* addon) {
        if (AtkStage.Instance()->ScreenSize.Height < addon->Size.Y + config.SystemConfigAdditionalHeight) {
            Services.ChatGui.PrintError("Unable to resize config window, height would be too big.", "[VanillaPlus] [BiggerConfigWindow]");
            Services.PluginLog.Warning("[BiggerConfigWindow] Unable to resize config window, height would be too big.");
            return;
        }
        
        addon->Size += new Vector2(0.0f, config.CharacterConfigAdditionalHeight);
        
        // Adjust "Default" "Apply" "Close" container
        addon->GetNodeById(35)->Position += new Vector2(0.0f, config.CharacterConfigAdditionalHeight);

        // Adjust vertical line ninegridnode
        addon->GetNodeById(34)->Size += new Vector2(0.0f, config.CharacterConfigAdditionalHeight);
    }

    private void FinalizeConfigCharacter(AtkUnitBase* addon) {
        addon->Size -= new Vector2(0.0f, config.CharacterConfigAdditionalHeight);
        
        // Adjust "Default" "Apply" "Close" container
        addon->GetNodeById(35)->Position -= new Vector2(0.0f, config.CharacterConfigAdditionalHeight);

        // Adjust vertical line ninegridnode
        addon->GetNodeById(34)->Size -= new Vector2(0.0f, config.CharacterConfigAdditionalHeight);
    }
    
    private void SetupConfigCharacterChild(AtkUnitBase* addon) {
        var scrollBarComponent = GetScrollbarForChild(addon);
        if (scrollBarComponent is null) return;
        
        ResizeHelpers.ResizeScrollBarNode(scrollBarComponent, config.CharacterConfigAdditionalHeight);
        
        // Adjust list area stop, only visible in certain themes
        addon->GetNodeById(5)->Position += new Vector2(0.0f, config.CharacterConfigAdditionalHeight);
    }

    private void FinalizeConfigCharacterChild(AtkUnitBase* addon) {
        var scrollBarComponent = GetScrollbarForChild(addon);
        if (scrollBarComponent is null) return;
        
        ResizeHelpers.ResizeScrollBarNode(scrollBarComponent, -config.CharacterConfigAdditionalHeight);
        
        // Adjust list area stop, only visible in certain themes
        addon->GetNodeById(5)->Position -= new Vector2(0.0f, config.CharacterConfigAdditionalHeight);
    }

    private AtkComponentScrollBar* GetScrollbarForChild(AtkUnitBase* addon)
        => (AtkComponentScrollBar*) Marshal.ReadIntPtr((nint) addon, sizeof(AtkUnitBase));
}
