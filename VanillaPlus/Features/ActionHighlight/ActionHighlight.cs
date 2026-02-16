using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ActionHighlight;

public unsafe class ActionHighlight : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ActionHighlight_DisplayName,
        Description = Strings.ActionHighlight_Description,
        Type = ModificationType.UserInterface,
        Authors = [ "attickdoor", "Zeffuro" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Fixed abilities like Ikishoten being highlighted when actually not usable"),
        ],
        CompatibilityModule = new PluginCompatibilityModule("AbilityAnts"),
    };

    public override string ImageName => "ActionHighlight.png";

    private Hook<ActionManager.Delegates.IsActionHighlighted>? onAntsHook;
    private ActionHighlightConfig? config;
    private ActionHighlightAddon? configWindow;

    private Dictionary<uint, Action>? cachedActions;

    // Needed for AST cards
    public static readonly Dictionary<uint, HashSet<int>> JobActionWhiteList = new() {
        [33] = [7444, 7445, 37018, 37023, 37024, 37025, 37026, 37027, 37028],
    };

    public override void OnEnable() {
        cachedActions = [];

        config = ActionHighlightConfig.Load();

        configWindow = new ActionHighlightAddon {
            Size = new Vector2(700.0f, 500.0f),
            InternalName = "ActionHighlightConfig",
            Title = Strings.ActionHighlight_Configuration,
            Config = config,
        };

        OpenConfigAction = configWindow.Toggle;

        onAntsHook = Services.Hooker.HookFromAddress<ActionManager.Delegates.IsActionHighlighted>(ActionManager.MemberFunctionPointers.IsActionHighlighted, OnActionHighlighted);
        onAntsHook?.Enable();

        CacheActions();
    }

    public override void OnDisable() {
        onAntsHook?.Dispose();
        onAntsHook = null;

        configWindow?.Dispose();
        configWindow = null;

        cachedActions = null;
    }

    private bool OnActionHighlighted(ActionManager* actionManager, ActionType actionType, uint actionId) {
        if (Services.ObjectTable.LocalPlayer is not { Level: var playerLevel, GameObjectId: var playerId } ) return false;
        if (config is null) return false;
        if (cachedActions is null) return false;

        var original = onAntsHook!.Original(actionManager, actionType, actionId);

        if (original) return original;
        if (actionType is not ActionType.Action) return original;

        if (!config.ActionSettings.TryGetValue(actionId, out var setting)) return original;
        if (!setting.IsEnabled) return original;

        if (actionManager->GetActionStatus(actionType, actionId, playerId, false) != 0) return original;

        if (config.ShowOnlyInCombat && !Services.Condition.IsInCombat) return original;
        if (!cachedActions.TryGetValue(actionId, out var action)) return original;

        var thresholdMs = config.UseGlocalPreAntMs ? config.PreAntTimeMs : setting.ThresholdMs;

        if (config.ShowOnlyUsableActions && action.ClassJobLevel > playerLevel)
            return original;

        var maxCharges = ActionManager.GetMaxCharges(actionId, playerLevel);
        var recastActive = actionManager->IsRecastTimerActive(actionType, actionId);
        var recastTime = actionManager->GetRecastTime(actionType, actionId);
        var recastElapsed = actionManager->GetRecastTimeElapsed(actionType, actionId);

        if (maxCharges is 0) {
            if (!recastActive) return true;
            return recastTime - recastElapsed <= thresholdMs / 1000f;
        }

        var currentCharges = actionManager->GetCurrentCharges(actionId);
        var perChargeRecast = ActionManager.GetAdjustedRecastTime(ActionType.Action, actionId) / 1000f;

        if (!config.AntOnlyOnFinalStack) {
            if (currentCharges > 0 && !recastActive) return true;
            var nextChargeBoundary = (currentCharges + 1) * perChargeRecast;
            var timeLeft = nextChargeBoundary - recastElapsed;
            return timeLeft <= thresholdMs / 1000f;
        }
        else {
            if (currentCharges >= maxCharges) return true;
            if (currentCharges < maxCharges - 1) return false;
            var finalChargeBoundary = maxCharges * perChargeRecast;
            var timeLeft = finalChargeBoundary - recastElapsed;
            return timeLeft <= thresholdMs / 1000f;
        }
    }

    private void CacheActions() {
        var actions = GetClassActions();

        foreach (var action in actions) {
            cachedActions?.TryAdd(action.RowId, action);
        }

        var roleActions = Services.DataManager.RoleActions.ToList();

        foreach (var roleAction in roleActions) {
            cachedActions?.TryAdd(roleAction.RowId, roleAction);
        }
    }

    public static List<Action> GetClassActions() {
        var whitelistedActions = JobActionWhiteList.Values.SelectMany(hashSet => hashSet).ToList();

        // I have no idea what Unknown6 is, but it's been in there since the very first AbilityAnts release.
        // My best guess at the moment is that it removes abilities that have been replaced or upgraded.
        return Services.DataManager.GetExcelSheet<Action>()
            .Where(action => IsValidAction(action) || whitelistedActions.Contains((int)action.RowId))
            .ToList();
    }

    private static bool IsValidAction(Action action)
        => action is { IsPvP: false, ClassJob.ValueNullable.Unknown6: > 0, IsPlayerAction: true } and ( { ActionCategory.RowId: 4 } or { Recast100ms: > 100 } );
}
