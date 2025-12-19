using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DebugGameModification;

#if DEBUG
/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Debug GameModification",
        Description = "A module for playing around and testing VanillaPlus features",
        Type = ModificationType.Debug,
        Authors = [ "YourNameHere" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override unsafe void OnEnable() {
        var addon = Services.GameGui.GetAddonByName<AtkUnitBase>("ConfigSystem");
        if (addon is not null) {
            Services.Framework.RunOnFrameworkThread(() => {
                Services.PluginLog.Debug($"CurrentAddress: {(nint)addon->VirtualTable:X}");
                
                var resolvedAddress = Services.AddonLifecycle.GetOriginalVirtualTable((nint)addon->VirtualTable);
                Services.PluginLog.Debug($"ResolvedAddress: {resolvedAddress:X}");
            });
        }
    }

    public override void OnDisable() {
    }
}
#endif
