using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Extensions;

/// <summary>
/// Extension methods for dalamud's IObjectTable service.
/// </summary>
public static class ObjectTableExtensions {
    extension(IObjectTable objectTable) {
        public IBattleChara? GetBattleChara(uint entityId)
            => objectTable.CharacterManagerObjects.FirstOrDefault(obj => obj.EntityId == entityId) as IBattleChara;
    }
}
