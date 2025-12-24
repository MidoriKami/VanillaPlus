using System.Collections.Generic;
using Dalamud.Game.Gui.NamePlate;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.HideDeadEnemyNamePlates;

public class HideDeadEnemyNamePlates : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Hide Dead Enemy Nameplates",
        Description = "Hides the nameplates of any enemies that are currently dead.",
        Type = ModificationType.GameBehavior,
        Authors = [ "nebel" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override void OnEnable() {
        Services.NamePlateGui.OnDataUpdate += OnNamePlateUpdate;
    }

    public override void OnDisable() {
        Services.NamePlateGui.OnDataUpdate -= OnNamePlateUpdate;
    }

    private static void OnNamePlateUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers) {
        foreach (var handler in handlers) {
            if (handler is { NamePlateKind: NamePlateKind.BattleNpcEnemy, GameObject.IsDead: true }) {
                handler.VisibilityFlags = 0;
            }
        }
    }
}
