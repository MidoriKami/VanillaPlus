using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using KamiToolKit.Components.Search;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.ActionHighlight.Config;

using AntsConfigAddon = KamiToolKit.Components.Configuration.TabbedConfigurationAddon<
    VanillaPlus.Features.ActionHighlight.Config.AntsClassJobConfig,
    VanillaPlus.Features.ActionHighlight.Nodes.AntsClassJobListItemNode,
    VanillaPlus.Features.ActionHighlight.Nodes.AntsClassJobConfigurationNode,
    VanillaPlus.Features.ActionHighlight.Nodes.AntsGeneralConfigurationNode>;

namespace VanillaPlus.Features.ActionHighlight;

public class ActionHighlight : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ActionHighlight_DisplayName,
        Description = Strings.ActionHighlight_Description,
        Type = ModificationType.UserInterface,
        Authors = ["attickdoor", "Zeffuro"],
        CompatibilityModule = new PluginCompatibilityModule("AbilityAnts"),
    };

    public override string ImageName => "ActionHighlight.png";
    public override bool IsExperimental => true;

    private Hook<ActionManager.Delegates.IsActionHighlighted>? onAntsHook;

    internal static AntsConfig? Config { get; private set; }
    private AntsConfigAddon? configAddon;

    private ClassJobSearchAddon? classJobSearchAddon;

    public override async Task OnEnableAsync() {
        Config = await AntsConfig.Load();

        classJobSearchAddon = new ClassJobSearchAddon {
            InternalName = "ClassJobSearch",
            Title = "Class Job Search",
            Size = new Vector2(300.0f, 535.0f),
            OptionsList = Service<IDataManager>.Get().GetExcelSheet<ClassJob>()
                .Where(job => job is { RowId: not 0, Name.IsEmpty: false, IsCrafter: false, IsGatherer: false })
                .ToList(),
            AllowMultiselect = true,
        };

        configAddon = new AntsConfigAddon {
            Size = new Vector2(700.0f, 650.0f),
            InternalName = "ActionHighlightConfig",
            Title = Strings.ActionHighlight_Configuration,
            OptionsList = [],
            SaveConfig = () => Task.Run(Config.Save),
            GetEntrySearchString = entry => Service<IDataManager>.Get().GetExcelSheet<ClassJob>().GetRow(entry.ClassJobId).Name.ToString(),
            AddClicked = OnAddClicked,
            RemoveClicked = OnRemoveClicked,
        };

        UpdateOptionsList();

        OpenConfigAction = configAddon.Toggle;

        unsafe {
            onAntsHook = Service<IGameInteropProvider>.Get().HookFromAddress<ActionManager.Delegates.IsActionHighlighted>(ActionManager.MemberFunctionPointers.IsActionHighlighted, OnActionHighlighted);
            onAntsHook?.Enable();
        }
    }

    public override async Task OnDisableAsync() {
        onAntsHook?.Dispose();
        onAntsHook = null;

        await Task.WhenAll(
            configAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            classJobSearchAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );
        configAddon = null;
        classJobSearchAddon = null;
    }

    private void OnAddClicked() {
        if (Config is null) return;

        classJobSearchAddon?.ConfirmedSelections = results => {
            foreach (var result in results) {
                if (Config.ClassJobConfigs.Any(entry => entry.ClassJobId == result.RowId)) continue;

                Config.ClassJobConfigs.Add(new AntsClassJobConfig {
                    ClassJobId = result.RowId,
                    ActionSettings = [],
                });
            }

            Task.Run(Config.Save);
            UpdateOptionsList();
        };

        classJobSearchAddon?.Open();
    }

    private void OnRemoveClicked(AntsClassJobConfig entry) {
        if (Config is null) return;

        Config.ClassJobConfigs.Remove(entry);
        Task.Run(Config.Save);
    }

    private unsafe bool OnActionHighlighted(ActionManager* actionManager, ActionType actionType, uint actionId) {
        if (Service<IObjectTable>.Get().LocalPlayer is not { Level: var playerLevel, GameObjectId: var playerId, ClassJob: var classJob }) return false;
        if (Config is null) return false;

        var original = onAntsHook!.Original(actionManager, actionType, actionId);
        if (original) return original;
        if (actionType is not ActionType.Action) return original;

        if (Config.ShowOnlyInCombat && !Service<ICondition>.Get().IsInCombat) return original;
        if (actionManager->GetActionStatus(actionType, actionId, playerId, false) != 0) return original;

        var action = Service<IDataManager>.Get().GetExcelSheet<Action>().GetRow(actionId);

        if (Config.ShowOnlyUsableActions && action.ClassJobLevel > playerLevel) return original;

        var classJobSettings = Config.ClassJobConfigs.FirstOrDefault(entry => entry.ClassJobId == classJob.RowId);
        if (classJobSettings is null) return original;

        var actionSettings = classJobSettings.ActionSettings.FirstOrDefault(actionSetting => actionSetting.ActionId ==  actionId);
        if (actionSettings is null) return original;

        if (!actionSettings.IsEnabled) return original;

        var thresholdMs = Config.UseGlocalPreAntMs ? Config.PreAntTimeMs : actionSettings.ThresholdMs;
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

        if (!Config.AntOnlyOnFinalStack) {
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

    private void UpdateOptionsList() {
        if (configAddon is null) return;
        if (Config is null) return;

        Task.Run(() => {
            var results = Config.ClassJobConfigs
                .Where(entry => entry.ClassJobId is not 0)
                .OrderBy(entry => Service<ISeStringEvaluator>.Get().EvaluateFromAddon(981, [entry.ClassJobId]).ToString())
                .ToList();

            Service<IFramework>.Get().RunSafely(() => {
                configAddon.OptionsList = results;
            });
        });
    }

    /// <summary>
    /// Gets all valid actions for the specified ClassJob.
    /// </summary>
    public static List<Action> GetClassActions(ClassJob classJob)
        => Service<IDataManager>.Get().GetExcelSheet<Action>()
            .Where(action => IsValidAction(action, classJob))
            .DistinctBy(action => action.RowId)
            .ToList();

    /// <summary>
    /// Returns true for actions that belong to the specified ClassJob <b>or its parent class</b>.
    /// </summary>
    private static bool IsPlayerClassAction(Action action, ClassJob classJob)
        => action.IsPlayerAction && action.ClassJobCategory.Value.IncludesJob(classJob);

    /// <summary>
    /// Returns true for actions that can be used, but only as flip actions from other skills, such as dancer steps.
    /// </summary>
    private static bool IsUnassignableClassAction(Action action, ClassJob classJob)
        => action is { IsPlayerAction: false, ClassJob.RowId: 0, ClassJobLevel: not 0 }
           && action.IsUsableByJob(classJob);

    /// <summary>
    /// Returns true for actions that we want to allow ActionHighlight to highlight.
    /// </summary>
    internal static bool IsValidAction(Action action, ClassJob classJob)
        => action is { IsPvP: false, IsRoleAction: false, RowId: not (2272u or 29581u or 1584u) } // Rabbit Medium and 六道輪廻 (should be a PVP action) and Purify
           && (action.ActionCategory.RowId == 4 || action.Recast100ms > 150)
           && (IsPlayerClassAction(action, classJob) || IsUnassignableClassAction(action, classJob));

    /// <summary>
    /// Returns true for actions that are valid for the specified ClassJob.
    /// </summary>
    internal static bool IsValidRoleAction(Action action, ClassJob classJob)
        => action is { IsPvP: false, IsRoleAction: true }
           && action.ClassJobCategory.Value.IncludesJob(classJob);
}
