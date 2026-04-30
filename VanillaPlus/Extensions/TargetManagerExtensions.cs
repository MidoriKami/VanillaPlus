using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Extensions;

public static class TargetManagerExtensions {
    extension(ITargetManager targetManager) {
        public IBattleChara? GetTarget()
            => targetManager.Target as IBattleChara ?? targetManager.SoftTarget as IBattleChara;

        public IBattleChara? GetFocusTarget()
            => targetManager.FocusTarget as IBattleChara;
    }
}
