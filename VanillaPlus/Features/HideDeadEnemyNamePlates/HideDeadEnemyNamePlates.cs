using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Game.Gui.NamePlate;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.HideDeadEnemyNamePlates;

public class HideDeadEnemyNamePlates : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_HideDeadEnemyNamePlates,
        Description = Strings.ModificationDescription_HideDeadEnemyNamePlates,
        Type = ModificationType.GameBehavior,
        Authors = ["nebel"],
    };

    public override Task OnEnableAsync() {
        Services.NamePlateGui.OnDataUpdate += OnNamePlateUpdate;

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        Services.NamePlateGui.OnDataUpdate -= OnNamePlateUpdate;

        return Task.CompletedTask;
    }

    private static void OnNamePlateUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers) {
        foreach (var handler in handlers) {
            if (handler is { NamePlateKind: NamePlateKind.BattleNpcEnemy, GameObject.IsDead: true }) {
                handler.VisibilityFlags = 0;
                handler.MarkerIconId = 0;
            }
        }
    }
}
