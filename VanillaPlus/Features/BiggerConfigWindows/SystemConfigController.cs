using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.BiggerConfigWindows;

public unsafe class SystemConfigController : IDisposable {
    private readonly BiggerConfigWindowsConfig config;
    
    private readonly AddonController systemConfigController;
    private readonly List<uint> scrollBarIdList = [ 17, 89, 286, 507 ];

    public SystemConfigController(BiggerConfigWindowsConfig config) {
        this.config = config;

        systemConfigController = new AddonController("ConfigSystem");
        systemConfigController.OnAttach += OnAttach;
        systemConfigController.OnDetach += OnDetach;
        systemConfigController.Enable();
    }

    public void Dispose() {
        systemConfigController.Dispose();
    }

    private void OnAttach(AtkUnitBase* addon) {
        addon->Size += new Vector2(0.0f, config.SystemConfigAdditionalHeight);

        // Adjust scrollable containers
        foreach (var scrollBarId in scrollBarIdList) {
            var scrollBarComponent = addon->GetComponentById<AtkComponentScrollBar>(scrollBarId);
            if (scrollBarComponent is null) continue;
            
            ResizeHelpers.ResizeScrollBarNode(scrollBarComponent, config.SystemConfigAdditionalHeight);
        }

        // Adjust "Default" "Apply" "Close" container
        addon->GetNodeById(585)->Position += new Vector2(0.0f, config.SystemConfigAdditionalHeight);

        // Adjust vertical line ninegridnode
        addon->GetNodeById(15)->Size += new Vector2(0.0f, config.SystemConfigAdditionalHeight);
    }
    
    private void OnDetach(AtkUnitBase* addon) {
        addon->Size -= new Vector2(0.0f, config.SystemConfigAdditionalHeight);

        // Adjust containers and scroll
        foreach (var scrollBarId in scrollBarIdList) {
            var scrollBarComponent = addon->GetComponentById<AtkComponentScrollBar>(scrollBarId);
            if (scrollBarComponent is null) continue;

            ResizeHelpers.ResizeScrollBarNode(scrollBarComponent, -config.SystemConfigAdditionalHeight);
        }

        // Adjust "Default" "Apply" "Close" container
        addon->GetNodeById(585)->Position -= new Vector2(0.0f, config.SystemConfigAdditionalHeight);

        // Adjust vertical line ninegridnode
        addon->GetNodeById(15)->Size -= new Vector2(0.0f, config.SystemConfigAdditionalHeight);
    }
}
