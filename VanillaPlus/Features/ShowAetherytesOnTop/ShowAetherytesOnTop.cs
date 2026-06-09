using System.Threading.Tasks;
using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using AgentId = Dalamud.Game.Agent.AgentId;

namespace VanillaPlus.Features.ShowAetherytesOnTop;

public class ShowAetherytesOnTop : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ShowAetherytesOnTop,
        Description = Strings.ModificationDescription_ShowAetherytesOnTop,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = ["MidoriKami"],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    private KeyStateFlags controlPreState;

    public override Task OnEnableAsync() {
        Services.AgentLifecycle.RegisterListener(AgentEvent.PreUpdate, AgentId.Map, OnMapPreUpdate);
        Services.AgentLifecycle.RegisterListener(AgentEvent.PostUpdate, AgentId.Map, OnMapPostUpdate);

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        Services.AgentLifecycle.UnregisterListener(OnMapPreUpdate, OnMapPostUpdate);

        // What's the worst that could happen ...
        unsafe {
            AgentMap.Instance()->IsControlKeyPressed = true;
        }

        return Task.CompletedTask;
    }

    private unsafe void OnMapPreUpdate(AgentEvent type, AgentArgs args) {
        if (AgentMap.Instance()->SelectedTerritoryId is 0) return;

        controlPreState = ControlKeyState;
        ControlKeyState = KeyStateFlags.Down | KeyStateFlags.Held;
    }

    private unsafe void OnMapPostUpdate(AgentEvent type, AgentArgs args) {
        if (AgentMap.Instance()->SelectedTerritoryId is 0) return;

        if (!controlPreState.HasFlag(KeyStateFlags.Down) && !controlPreState.HasFlag(KeyStateFlags.Held)) {
            ControlKeyState = KeyStateFlags.None;
            args.GetAgentPointer<AgentMap>()->IsControlKeyPressed = false;
        }
    }

    private static unsafe KeyStateFlags ControlKeyState {
        get => UIModule.Instance()->GetUIInputData()->KeyboardInputs.KeyState[(int)SeVirtualKey.CONTROL];
        set => UIModule.Instance()->GetUIInputData()->KeyboardInputs.KeyState[(int)SeVirtualKey.CONTROL] = value;
    }
}
