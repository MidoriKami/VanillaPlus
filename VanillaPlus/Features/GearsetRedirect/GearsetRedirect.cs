using System;
using System.Linq;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.GearsetRedirect;

public unsafe class GearsetRedirect : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_GearsetRedirect"),
        Description = Strings("ModificationDescription_GearsetRedirect"),
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Completely new configuration system, old configurations are no longer valid"),
        ],
    };

    private Hook<RaptureGearsetModule.Delegates.EquipGearset>? gearsetChangedHook;
    private GearsetRedirectConfig? config;
    private GearsetRedirectConfigAddon? configWindow;

    public override bool IsExperimental => true;

    public override void OnEnable() {
        config = GearsetRedirectConfig.Load();

        configWindow = new GearsetRedirectConfigAddon {
            Size = new Vector2(600.0f, 525.0f),
            InternalName = "GearsetRedirectConfig",
            Title = Strings("GearsetRedirect_ConfigTitle"),
            Config = config,
        };

        OpenConfigAction = () => {
            if (Services.ClientState.IsLoggedIn) {
                configWindow.Toggle();
            }
        };
        
        gearsetChangedHook = Services.Hooker.HookFromAddress<RaptureGearsetModule.Delegates.EquipGearset>(RaptureGearsetModule.Addresses.EquipGearset.Value, OnGearsetChanged);
        gearsetChangedHook?.Enable();
    }
    
    public override void OnDisable() {
        gearsetChangedHook?.Dispose();
        gearsetChangedHook = null;
        
        configWindow?.Dispose();
        configWindow = null;

        config = null;
    }

    private int OnGearsetChanged(RaptureGearsetModule* thisPtr, int gearsetId, byte glamourPlateId) {
        try {
            if (config is not null && config.Redirections.TryGetValue(gearsetId, out var redirection)) {
                var targetRedirection = redirection.FirstOrDefault(info => Services.ClientState.TerritoryType == info.TerritoryType);
                if (targetRedirection is not null) {
                    gearsetId = targetRedirection.AlternateGearsetId;
                }
            }
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Exception while handling Gearset Redirect.");
        }

        return gearsetChangedHook!.Original(thisPtr, gearsetId, glamourPlateId);
    }
}
