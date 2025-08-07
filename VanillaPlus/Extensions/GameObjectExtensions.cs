using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;

namespace VanillaPlus.Extensions;

public static class GameObjectExtensions {
    public static bool IsPet(this IGameObject gameObject)
        => gameObject is { ObjectKind: ObjectKind.BattleNpc, SubKind: (byte)BattleNpcSubKind.Pet };
}
