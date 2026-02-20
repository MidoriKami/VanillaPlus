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
    private readonly List<AddonController> childAddonControllers = [];

    // Doesn't seem like there's a way around this, the parent addon doesn't appear to have a reference to its children.
    private readonly List<string> childAddons = [ "ConfigCharaOpeGeneral", "ConfigCharaOpeTarget", "ConfigCharaOpeCircle", "ConfigCharaOpeChara", 
        "ConfigCharaItem", "ConfigCharacterHudGeneral", "ConfigCharacterHudHud", "ConfigCharaNamePlateOthers", "ConfigCharaNamePlateNpc", 
        "ConfigCharaNamePlateGen", "ConfigCharaHotbarDisplay", "ConfigCharaHotbarXHB", "ConfigCharaHotbarXHBCustom", "ConfigCharaChatLogGen", 
        "ConfigCharaChatLogDetail", "ConfigCharaChatLogRing",
    ];
    
    public CharacterConfigController(BiggerConfigWindowsConfig config) {
        this.config = config;
        characterConfigController = new AddonController("ConfigCharacter");
        characterConfigController.OnAttach += OnAttach;
        characterConfigController.OnDetach += OnDetach;
        characterConfigController.Enable();

        foreach (var addonName in childAddons) {
            var newAddonController = new AddonController(addonName);
            newAddonController.OnAttach += OnChildAttach;
            newAddonController.OnDetach += OnChildDetach;
            newAddonController.Enable();
            childAddonControllers.Add(newAddonController);
        }
    }

    public void Dispose() {
        foreach (var addonController in childAddonControllers) {
            addonController.Dispose();
        }
        childAddonControllers.Clear();
        
        characterConfigController.Dispose();
    }

    private void OnAttach(AtkUnitBase* addon) {
        addon->Size += new Vector2(0.0f, config.CharacterConfigAdditionalHeight);
        
        // Adjust "Default" "Apply" "Close" container
        addon->GetNodeById(35)->Position += new Vector2(0.0f, config.CharacterConfigAdditionalHeight);

        // Adjust vertical line ninegridnode
        addon->GetNodeById(34)->Size += new Vector2(0.0f, config.CharacterConfigAdditionalHeight);

    }

    private void OnDetach(AtkUnitBase* addon) {
        addon->Size -= new Vector2(0.0f, config.CharacterConfigAdditionalHeight);
        
        // Adjust "Default" "Apply" "Close" container
        addon->GetNodeById(35)->Position -= new Vector2(0.0f, config.CharacterConfigAdditionalHeight);

        // Adjust vertical line ninegridnode
        addon->GetNodeById(34)->Size -= new Vector2(0.0f, config.CharacterConfigAdditionalHeight);
    }
    
    private void OnChildAttach(AtkUnitBase* addon) {
        var scrollBarComponent = GetScrollbarForChild(addon);
        if (scrollBarComponent is null) return;
        
        ResizeHelpers.ResizeScrollBarNode(scrollBarComponent, config.CharacterConfigAdditionalHeight);
    }

    private void OnChildDetach(AtkUnitBase* addon) {
        var scrollBarComponent = GetScrollbarForChild(addon);
        if (scrollBarComponent is null) return;
        
        ResizeHelpers.ResizeScrollBarNode(scrollBarComponent, -config.CharacterConfigAdditionalHeight);
    }

    private AtkComponentScrollBar* GetScrollbarForChild(AtkUnitBase* addon)
        => (AtkComponentScrollBar*) Marshal.ReadIntPtr((nint) addon, sizeof(AtkUnitBase));
}
