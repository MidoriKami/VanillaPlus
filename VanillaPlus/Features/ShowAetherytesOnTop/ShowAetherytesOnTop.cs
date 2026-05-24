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

    public override async Task OnEnableAsync() {
        await Services.Framework.Run(() => {
            Services.AgentLifecycle.RegisterListener(AgentEvent.PreUpdate, AgentId.Map, OnMapPreUpdate);
            Services.AgentLifecycle.RegisterListener(AgentEvent.PostUpdate, AgentId.Map, OnMapPostUpdate);
        });
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            Services.AgentLifecycle.UnregisterListener(OnMapPreUpdate, OnMapPostUpdate);

            unsafe {
                AgentMap.Instance()->IsControlKeyPressed = true;
            }
        });
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
