using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Extensions;

public static class GameObjectExtensions {
    extension(IGameObject gameObject) {
        public bool IsPet => gameObject is { ObjectKind: ObjectKind.BattleNpc, SubKind: (byte)BattleNpcSubKind.Pet };
        public bool IsPetOwner => Services.ObjectTable.CharacterManagerObjects.Any(obj => obj.IsPet && obj.OwnerId == gameObject.EntityId);
        public bool IsBoss => Services.DataManager.GetExcelSheet<BNpcBase>().GetRow(gameObject.BaseId).Rank is 2 or 6;
        public bool IsAggroed => gameObject.TargetObjectId is not 0xE0000000;
    }

    extension(BattleChara battleChara) {
        public bool IsBoss => Services.DataManager.GetExcelSheet<BNpcBase>().GetRow(battleChara.BaseId).Rank is 2 or 6;
        public bool IsAggroed => battleChara.GetTargetId().ObjectId is not 0xE0000000;
    }
}
