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

    private ActionManager* actionManager;

    // Needed for AST cards
    public static readonly Dictionary<uint, HashSet<int>> JobActionWhiteList = new()
    {
        { 33, [7444, 7445, 37018, 37023, 37024, 37025, 37026, 37027, 37028] },
    };

    public override void OnEnable() {
        cachedActions = [];

        config = ActionHighlightConfig.Load();
        configWindow = new ActionHighlightAddon() {
            Size = new Vector2(700.0f, 500.0f),
            InternalName = "ActionHighlightConfig",
            Title = Strings("ActionHighlightConfig_ConfigTitle"),
            Config = config,
        };

        OpenConfigAction = configWindow.Toggle;

        actionManager = ActionManager.Instance();
        onAntsHook = Services.Hooker.HookFromAddress<ActionManager.Delegates.IsActionHighlighted>(ActionManager.MemberFunctionPointers.IsActionHighlighted, OnActionHighlighted);
        onAntsHook?.Enable();
        CacheActions();
    }

    public override void OnDisable() {
        onAntsHook?.Disable();
        onAntsHook = null;

        cachedActions = null;
    }

    private bool OnActionHighlighted(ActionManager* _, ActionType actionType, uint actionId) {
        if (Services.ObjectTable.LocalPlayer == null || config == null || cachedActions == null)
            return false;

        var original = onAntsHook!.Original(actionManager, actionType, actionId);

        if (original || actionType != ActionType.Action)
            return original;

        if (config.ShowOnlyInCombat && !Services.Condition.IsInCombat) return original;

        if (!config.ActiveActions.TryGetValue(actionId, out var thresholdMs) ||
            !cachedActions.TryGetValue(actionId, out var action)) {
            return original;
        }

        thresholdMs = config.UseGlocalPreAntMs ? config.PreAntTimeMs : thresholdMs;

        if (config.ShowOnlyUsableActions && action.ClassJobLevel > Services.ObjectTable.LocalPlayer.Level)
            return false;

        var maxCharges = ActionManager.GetMaxCharges(actionId, Services.ObjectTable.LocalPlayer.Level);
        var recastActive = actionManager->IsRecastTimerActive(actionType, actionId);

        if (maxCharges <= 1) {
            if (!recastActive) return true;
            var timeLeft = actionManager->GetRecastTimeLeft(actionType, actionId);
            return timeLeft <= thresholdMs / 1000f;
        }

        var currentCharges = actionManager->GetCurrentCharges(actionId);

        if (currentCharges > 0 && !config.AntOnlyOnFinalStack)
        {
            return true;
        }

        var timer = actionManager->GetRecastDetail(action);
        if (timer == null) return original;

        var chargeTime = timer->Total / maxCharges;
        var timeIntoCurrentCharge = timer->Elapsed % chargeTime;
        var timeUntilNextCharge = chargeTime - timeIntoCurrentCharge;

        return timeUntilNextCharge <= thresholdMs / 1000f;
    }

    private void CacheActions()
    {
        var actions = GetClassActions();

        foreach (var action in actions)
        {
            cachedActions?.TryAdd(action.RowId, action);
        }

        var roleActions = Services.DataManager.GetRoleActions().ToList();

        foreach (var roleAction in roleActions)
        {
            cachedActions?.TryAdd(roleAction.RowId, roleAction);
        }
    }

    public static List<Action> GetClassActions()
    {
        var whitelistedActions = JobActionWhiteList.Values.SelectMany(hashSet => hashSet).ToList();

        // I have no idea what Unknown6 is, but it's been in there since the very first AbilityAnts release.
        // My best guess at the moment is that it removes abilities that have been replaced or upgraded.
        return Services.DataManager.GetExcelSheet<Action>()!
            .Where(a =>
                (a is { IsPvP: false, ClassJob.ValueNullable.Unknown6: > 0, IsPlayerAction: true } &&
                 (a.ActionCategory.RowId == 4 || a.Recast100ms > 100))
                || whitelistedActions.Contains((int)a.RowId)) // Include whitelisted actions
            .ToList();
    }
}
