using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.DutyState;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FocusTargetLock;

public class FocusTargetLock : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FocusTargetLock,
        Description = Strings.ModificationDescription_FocusTargetLock,
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
    };

    private uint? targetBaseId;
    private uint? targetEntityId;

    public override void OnEnable() {
        Services.DutyState.DutyWiped += OnDutyWiped;
        Services.DutyState.DutyRecommenced += OnDutyRecommenced;
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public override void OnDisable() {
        Services.DutyState.DutyWiped -= OnDutyWiped;
        Services.DutyState.DutyRecommenced -= OnDutyRecommenced;
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
    }

    private void OnDutyRecommenced(IDutyStateEventArgs args) {
        if (targetBaseId is null || targetEntityId is null) return;

        IGameObject? targetObject;

        // BaseId works well for reacquiring bosses, but doesn't work for other players.
        if (targetBaseId is not 0) { 
            targetObject = Services.ObjectTable.CharacterManagerObjects.FirstOrDefault(obj => obj.BaseId == targetBaseId);
        }
        
        // Player EntityId's never change once in an instance, but enemies do, so this works to reacquire players.
        else {
            targetObject = Services.ObjectTable.CharacterManagerObjects.FirstOrDefault(obj => obj.EntityId == targetEntityId);
        }

        Services.TargetManager.FocusTarget = targetObject;
    }

    private void OnDutyWiped(IDutyStateEventArgs args) {
        targetBaseId = Services.TargetManager.FocusTarget?.BaseId;
        targetEntityId = Services.TargetManager.FocusTarget?.EntityId;
    }

    private void OnTerritoryChanged(uint u) {
        targetBaseId = null;
        targetEntityId = null;
    }
}
