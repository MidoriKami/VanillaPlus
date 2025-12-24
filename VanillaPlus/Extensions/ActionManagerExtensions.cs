using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Extensions;

public static unsafe class ActionManagerExtensions {
    extension(ref ActionManager actionManager) {
        public float GetRecastTimeLeft(ActionType actionType, uint actionId)
            => actionManager.GetRecastTime(actionType, actionId) - actionManager.GetRecastTimeElapsed(actionType, actionId);

        /// <summary>
        /// Gets the RecastDetails for the specified action, CooldownGroup 58 is seemingly used for actions that both have their own cooldown in addition to adhering to GCD.
        /// </summary>
        public RecastDetail* GetRecastDetail(Action action) {
            var group = action.CooldownGroup == 58 ? action.AdditionalCooldownGroup : actionManager.GetRecastGroup(1, action.RowId);
            return actionManager.GetRecastGroupDetail(group);
        }
    }
}
