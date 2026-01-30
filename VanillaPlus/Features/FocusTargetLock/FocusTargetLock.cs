using System.Linq;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.FocusTargetLock;

public class FocusTargetLock : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FocusTargetLock,
        Description = Strings.ModificationDescription_FocusTargetLock,
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private uint? targetBaseId;

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

    private void OnDutyRecommenced(object? sender, ushort e) {
        if (targetBaseId is null) return;

        var desiredTarget = Services.ObjectTable.CharacterManagerObjects.FirstOrDefault(obj => obj.BaseId == targetBaseId);
        if (desiredTarget is null) return;

        Services.TargetManager.FocusTarget = desiredTarget;
    }

    private void OnDutyWiped(object? sender, ushort e)
        => targetBaseId = Services.TargetManager.FocusTarget?.BaseId;

    private void OnTerritoryChanged(ushort obj)
        => targetBaseId = null;
}
