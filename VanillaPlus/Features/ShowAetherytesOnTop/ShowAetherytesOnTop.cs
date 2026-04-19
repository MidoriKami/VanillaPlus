using Dalamud.Game.Agent;
using Dalamud.Game.Agent.AgentArgTypes;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using AgentId = Dalamud.Game.Agent.AgentId;

namespace VanillaPlus.Features.ShowAetherytesOnTop;

public unsafe class ShowAetherytesOnTop : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ShowAetherytesOnTop,
        Description = Strings.ModificationDescription_ShowAetherytesOnTop,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Map,
        Authors = [ "MidoriKami" ],
        CompatibilityModule = new QuestAwayCompatabilityModule(),
    };

    private KeyStateFlags controlPreState;

    public override void OnEnable() {
        Services.AgentLifecycle.RegisterListener(AgentEvent.PreUpdate, AgentId.Map, OnMapPreUpdate);
        Services.AgentLifecycle.RegisterListener(AgentEvent.PostUpdate, AgentId.Map, OnMapPostUpdate);
    }

    public override void OnDisable() {
        Services.AgentLifecycle.UnregisterListener(OnMapPreUpdate, OnMapPostUpdate);
        AgentMap.Instance()->IsControlKeyPressed = true;
    }

    private void OnMapPreUpdate(AgentEvent type, AgentArgs args) {
        controlPreState = ControlKeyState;
        ControlKeyState = KeyStateFlags.Down | KeyStateFlags.Held;
    }

    private void OnMapPostUpdate(AgentEvent type, AgentArgs args) {
        if (!controlPreState.HasFlag(KeyStateFlags.Down) && !controlPreState.HasFlag(KeyStateFlags.Held)) {
            ControlKeyState = KeyStateFlags.None;
            args.GetAgentPointer<AgentMap>()->IsControlKeyPressed = false;
        }
    }

    private static KeyStateFlags ControlKeyState {
        get => UIModule.Instance()->GetUIInputData()->KeyboardInputs.KeyState[(int)SeVirtualKey.CONTROL];
        set => UIModule.Instance()->GetUIInputData()->KeyboardInputs.KeyState[(int)SeVirtualKey.CONTROL] = value;
    }
}
