using System.Globalization;
using Dalamud.Game.ClientState.Objects.Types;

namespace VanillaPlus.Extensions;

/// <summary>
/// Extension methods for IBattleChara
/// </summary>
public static class BattleCharaExtensions {
    extension(IBattleChara battleChara) {
        public string GetCastTimeString {
            get {
                if (!battleChara.IsValid()) return string.Empty;
                if (battleChara.EntityId is 0xE0000000) return string.Empty;
                if (battleChara.CurrentCastTime >= battleChara.TotalCastTime) return string.Empty;

                return (battleChara.TotalCastTime - battleChara.CurrentCastTime).ToString("00.00", CultureInfo.InvariantCulture);
            }
        }
    }
}
