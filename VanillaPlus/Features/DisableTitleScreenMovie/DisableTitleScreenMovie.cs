using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.DisableTitleScreenMovie;

public unsafe class DisableTitleScreenMovie : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_DisableTitleScreenMovie,
        Description = Strings.ModificationDescription_DisableTitleScreenMovie,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
        CompatibilityModule = new SimpleTweaksCompatibilityModule("DisableTitleScreenMovie"),
    };

    private Hook<AgentLobby.Delegates.UpdateLobbyUIStage>? updateTitleScreenHook;

    public override Task OnEnableAsync() {
        updateTitleScreenHook = Services.Hooker.HookFromAddress<AgentLobby.Delegates.UpdateLobbyUIStage>(AgentLobby.MemberFunctionPointers.UpdateLobbyUIStage, OnTitleScreenUpdate);
        updateTitleScreenHook?.Enable();

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        updateTitleScreenHook?.Dispose();
        updateTitleScreenHook = null;

        return Task.CompletedTask;
    }

    private void OnTitleScreenUpdate(AgentLobby* thisPtr) {
        try {
            if (thisPtr->LobbyUIStage is 9) {
                var flagValue = Marshal.ReadInt64((nint)thisPtr, 0x1378) & 0xFF; // todo: need to figure out how to add this to CS
                if (flagValue is 0) {
                    return;
                }
            }

            updateTitleScreenHook!.Original(thisPtr);
        }
        catch (Exception e) {
            Services.PluginLog.Exception(e);
        }
    }
}
