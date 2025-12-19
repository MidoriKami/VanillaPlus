using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;

namespace VanillaPlus.Extensions;

public static class GameObjectExtensions {
    extension(IGameObject gameObject) {
        public bool IsPet => gameObject is { ObjectKind: ObjectKind.BattleNpc, SubKind: (byte)BattleNpcSubKind.Pet };
        public bool IsPetOwner => Services.ObjectTable.CharacterManagerObjects.Any(obj => obj.IsPet && obj.OwnerId == gameObject.EntityId);
    }
}
