using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.DutyState;
using Dalamud.Plugin.Services;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FocusTargetLock;

public class FocusTargetLock : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FocusTargetLock,
        Description = Strings.ModificationDescription_FocusTargetLock,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
    };

    private uint? targetBaseId;
    private uint? targetEntityId;

    public override Task OnEnableAsync() {
        IDutyState.Get().DutyWiped += OnDutyWiped;
        IDutyState.Get().DutyRecommenced += OnDutyRecommenced;
        IClientState.Get().TerritoryChanged += OnTerritoryChanged;

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        IDutyState.Get().DutyWiped -= OnDutyWiped;
        IDutyState.Get().DutyRecommenced -= OnDutyRecommenced;
        IClientState.Get().TerritoryChanged -= OnTerritoryChanged;

        return Task.CompletedTask;
    }

    private void OnDutyRecommenced(IDutyStateEventArgs args) {
        if (targetBaseId is null || targetEntityId is null) return;

        IGameObject? targetObject;

        // BaseId works well for reacquiring bosses, but doesn't work for other players.
        if (targetBaseId is not 0) {
            targetObject = IObjectTable.Get().CharacterManagerObjects.FirstOrDefault(obj => obj.BaseId == targetBaseId);
        }

        // Player EntityId's never change once in an instance, but enemies do, so this works to reacquire players.
        else {
            targetObject = IObjectTable.Get().CharacterManagerObjects.FirstOrDefault(obj => obj.EntityId == targetEntityId);
        }

        ITargetManager.Get().FocusTarget = targetObject;
    }

    private void OnDutyWiped(IDutyStateEventArgs args) {
        targetBaseId = ITargetManager.Get().FocusTarget?.BaseId;
        targetEntityId = ITargetManager.Get().FocusTarget?.EntityId;
    }

    private void OnTerritoryChanged(uint u) {
        targetBaseId = null;
        targetEntityId = null;
    }
}
