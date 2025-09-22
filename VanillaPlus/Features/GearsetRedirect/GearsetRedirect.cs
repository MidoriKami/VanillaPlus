using System;
using System.Linq;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.GearsetRedirect;

public unsafe class GearsetRedirect : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Gearset Redirect",
        Description = "When equipping gearsets, set alternative sets to load depending on what zone you are in.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private Hook<RaptureGearsetModule.Delegates.EquipGearset>? gearsetChangedHook;
    private GearsetRedirectConfig? config;
    private GearsetRedirectConfigWindow? configWindow;
    
    public override void OnEnable() {
        config = GearsetRedirectConfig.Load();

        configWindow = new GearsetRedirectConfigWindow(config);
        configWindow.AddToWindowSystem();
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
        
        configWindow?.RemoveFromWindowSystem();
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
