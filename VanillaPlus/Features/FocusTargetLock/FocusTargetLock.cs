using Dalamud.Game.ClientState.Objects.Types;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.FocusTargetLock;

public class FocusTargetLock : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_FocusTargetLock"),
        Description = Strings("ModificationDescription_FocusTargetLock"),
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private IGameObject? focusTarget;

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

    private void OnDutyRecommenced(object? sender, ushort e)
        => Services.TargetManager.FocusTarget = focusTarget;

    private void OnDutyWiped(object? sender, ushort e)
        => focusTarget = Services.TargetManager.FocusTarget;

    private void OnTerritoryChanged(ushort obj)
        => focusTarget = null;
}
