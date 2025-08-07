using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;

namespace VanillaPlus.Extensions;

public static class ConditionExtensions {
    public static bool IsBoundByDuty(this ICondition condition) 
        => condition.Any(ConditionFlag.BoundByDuty, ConditionFlag.BoundByDuty56, ConditionFlag.BoundByDuty95);

    public static bool IsInCombat(this ICondition condition)
        => condition.Any(ConditionFlag.InCombat);

    public static bool IsInCutscene(this ICondition condition)
        => condition.Any(ConditionFlag.OccupiedInCutSceneEvent, ConditionFlag.WatchingCutscene, ConditionFlag.WatchingCutscene78);

    public static bool IsBetweenAreas(this ICondition condition)
        => condition.Any(ConditionFlag.BetweenAreas, ConditionFlag.BetweenAreas51);

    public static bool IsCrafting(this ICondition condition)
        => condition.Any(ConditionFlag.Crafting, ConditionFlag.ExecutingCraftingAction, ConditionFlag.PreparingToCraft);

    public static bool IsCrossWorld(this ICondition condition)
        => condition.Any(ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance);

    public static bool IsGathering(this ICondition condition)
        => condition.Any(ConditionFlag.Gathering, ConditionFlag.ExecutingGatheringAction);

    public static bool IsInBardPerformance(this ICondition condition)
        => condition.Any(ConditionFlag.Performing);

    public static unsafe bool IsIslandDoingSomethingMode()
        => MJIManager.Instance()->CurrentMode is not 0 && MJIManager.Instance()->IsPlayerInSanctuary;

    public static bool IsInQuestEvent(this ICondition condition)
        => condition.Any(ConditionFlag.OccupiedInQuestEvent) || IsIslandDoingSomethingMode();

    public static bool IsInCutsceneOrQuestEvent(this ICondition condition)
        => condition.IsInCutscene() || condition.IsInQuestEvent();

    public static bool IsDutyRecorderPlayback(this ICondition condition)
        => condition.Any(ConditionFlag.DutyRecorderPlayback);
}
