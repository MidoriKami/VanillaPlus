using System;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DisableTitleScreenMovie;

public unsafe class SampleGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Disable Title Screen Movie",
        Description = "Prevents the title screen from playing the introduction movie.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("DisableTitleScreenMovie"),
    };

    private Hook<AgentLobby.Delegates.UpdateLobbyUIStage>? updateTitleScreenHook;
    
    public override void OnEnable() {
        updateTitleScreenHook = Services.Hooker.HookFromAddress<AgentLobby.Delegates.UpdateLobbyUIStage>(AgentLobby.MemberFunctionPointers.UpdateLobbyUIStage, OnTitleScreenUpdate);
        updateTitleScreenHook?.Enable();
    }

    public override void OnDisable() {
        updateTitleScreenHook?.Dispose();
        updateTitleScreenHook = null;
    }

    private void OnTitleScreenUpdate(AgentLobby* thisPtr) {
        try {
            if (thisPtr->LobbyUIStage is 9) {
                var flagValue = Marshal.ReadInt64((nint)thisPtr, 0x1318) & 0xFF;
                if (flagValue is 0) {
                    return;
                }
            }
            
            updateTitleScreenHook!.Original(thisPtr);
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Exception in OnTitleScreenUpdate");
        }
    }
}
