﻿using Dalamud.Game.ClientState.Objects.Types;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.FocusTargetLock;

public class FocusTargetLock : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Focus Target Lock",
        Description = "When a duty recommences, restores your previous focus target.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private IGameObject? focusTarget;

    public override void OnEnable() {
        Services.DutyState.DutyWiped += OnDutyWiped;
        Services.DutyState.DutyRecommenced += OnDutyRecommenced;
    }

    public override void OnDisable() {
        Services.DutyState.DutyWiped -= OnDutyWiped;
        Services.DutyState.DutyRecommenced -= OnDutyRecommenced;
    }

    private void OnDutyRecommenced(object? sender, ushort e)
        => Services.TargetManager.FocusTarget = focusTarget;

    private void OnDutyWiped(object? sender, ushort e)
        => focusTarget = Services.TargetManager.FocusTarget;
}
