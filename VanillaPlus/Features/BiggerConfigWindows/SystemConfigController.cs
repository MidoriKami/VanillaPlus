using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.BiggerConfigWindows;

public class SystemConfigController : IAsyncDisposable {
    public required BiggerConfigWindowsConfig Config { get; init; }

    private AddonController? systemConfigController;
    private readonly List<uint> scrollBarIdList = [17, 89, 286, 507];

    public async ValueTask DisposeAsync()
        => await DisableAsync();

    public async Task EnableAsync() {
        unsafe {
            systemConfigController = new AddonController {
                AddonName = "ConfigSystem",
                OnSetup = SetupConfigSystem,
                OnFinalize = FinalizeConfigSystem,
            };
        }

        await systemConfigController.EnableAsync();
    }

    public async Task DisableAsync() {
        if (systemConfigController is not null) {
            await systemConfigController.DisableAsync();
            systemConfigController = null;
        }
    }

    private unsafe void SetupConfigSystem(AtkUnitBase* addon) {
        if (AtkStage.Instance()->ScreenSize.Height < addon->Size.Y + Config.SystemConfigAdditionalHeight) {
            Services.ChatGui.PrintError("[BiggerConfigWindow] Unable to resize config window, height would be too big.", "VanillaPlus");
            Services.PluginLog.Warning("Unable to resize config window, height would be too big.", "BiggerConfigWindow");
            return;
        }

        addon->Size += new Vector2(0.0f, Config.SystemConfigAdditionalHeight);

        // Adjust scrollable containers
        foreach (var scrollBarId in scrollBarIdList) {
            var scrollBarComponent = addon->GetComponentById<AtkComponentScrollBar>(scrollBarId);
            if (scrollBarComponent is null) continue;

            ResizeHelpers.ResizeScrollBarNode(scrollBarComponent, Config.SystemConfigAdditionalHeight);
        }

        // Adjust "Default" "Apply" "Close" container
        addon->GetNodeById(588)->Position += new Vector2(0.0f, Config.SystemConfigAdditionalHeight);

        // Adjust vertical line ninegridnode
        addon->GetNodeById(15)->Size += new Vector2(0.0f, Config.SystemConfigAdditionalHeight);
    }

    private unsafe void FinalizeConfigSystem(AtkUnitBase* addon) {
        addon->Size -= new Vector2(0.0f, Config.SystemConfigAdditionalHeight);

        // Adjust containers and scroll
        foreach (var scrollBarId in scrollBarIdList) {
            var scrollBarComponent = addon->GetComponentById<AtkComponentScrollBar>(scrollBarId);
            if (scrollBarComponent is null) continue;

            ResizeHelpers.ResizeScrollBarNode(scrollBarComponent, -Config.SystemConfigAdditionalHeight);
        }

        // Adjust "Default" "Apply" "Close" container
        addon->GetNodeById(588)->Position -= new Vector2(0.0f, Config.SystemConfigAdditionalHeight);

        // Adjust vertical line ninegridnode
        addon->GetNodeById(15)->Size -= new Vector2(0.0f, Config.SystemConfigAdditionalHeight);
    }
}
