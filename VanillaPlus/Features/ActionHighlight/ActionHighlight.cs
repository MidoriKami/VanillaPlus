using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ActionHighlight;

public unsafe class ActionHighlight : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("Action Highlight"),
        Description = "Highlights abilities with the ants effect when they are off cooldown or shortly before they become available",
        Type = ModificationType.UserInterface,
        Authors = [ "Zeffuro" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
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
            Title = "Action Highlight Configuration",
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
        if (Services.ObjectTable.LocalPlayer is not { Level: var playerLevel } ) return false;
        if (config is null) return false;
        if (cachedActions is null) return false;
        
        var original = onAntsHook!.Original(actionManager, actionType, actionId);

        if (original) return original;
        if (actionType is not ActionType.Action) return original;
        if (config.ShowOnlyInCombat && !Services.Condition.IsInCombat) return original;
        if (!config.ActiveActions.TryGetValue(actionId, out var thresholdMs)) return original;
        if (!cachedActions.TryGetValue(actionId, out var action)) return original;

        if (config.UseGlocalPreAntMs)
            thresholdMs = config.PreAntTimeMs;

        if (config.ShowOnlyUsableActions && action.ClassJobLevel > playerLevel)
            return original;

        var maxCharges = ActionManager.GetMaxCharges(actionId, playerLevel);
        var recastActive = actionManager->IsRecastTimerActive(actionType, actionId);
        var recastTime = actionManager->GetRecastTime(actionType, actionId);
        var recastElapsed = actionManager->GetRecastTimeElapsed(actionType, actionId);

        if (maxCharges is 0) {
            if (! recastActive) return true;
            return recastTime - recastElapsed <= thresholdMs / 1000f;
        }

        if (!config.AntOnlyOnFinalStack) {
            var currentCharges = actionManager->GetCurrentCharges(actionId);
            if (currentCharges > 0 && !recastActive) return true;
            recastTime /= maxCharges;
        }

        var timeLeft = recastTime - recastElapsed;
        return timeLeft <= thresholdMs / 1000f;
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
