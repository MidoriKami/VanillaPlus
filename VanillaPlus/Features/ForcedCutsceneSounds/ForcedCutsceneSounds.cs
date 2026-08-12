using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;
using Task = System.Threading.Tasks.Task;

namespace VanillaPlus.Features.ForcedCutsceneSounds;

public class ForcedCutsceneSounds : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ForcedCutsceneSounds,
        Description = Strings.ModificationDescription_ForcedCutsceneSounds,
        Authors = ["Haselnussbomber", "MidoriKami"],
        Type = ModificationType.GameBehavior,
        CompatibilityModule = new HaselTweaksCompatibilityModule("ForcedCutsceneMusic"),
    };

    private static readonly string[] ConfigOptions = [
        "IsSndMaster",
        "IsSndBgm",
        "IsSndSe",
        "IsSndVoice",
        "IsSndEnv",
        "IsSndSystem",
        "IsSndPerform",
    ];

    private Dictionary<string, bool>? wasMuted;

    private ForcedCutsceneSoundsConfig? config;
    private ConfigAddon? configWindow;

    private Hook<EventSceneModuleTaskManager.Delegates.AddTask>? addTaskHook;

    public override async Task OnEnableAsync() {
        unsafe {
            addTaskHook = IGameInteropProvider.Get().HookFromAddress<EventSceneModuleTaskManager.Delegates.AddTask>(EventSceneModuleTaskManager.Addresses.AddTask.Value, OnTaskAdded);
            addTaskHook?.Enable();
        }

        wasMuted = [];

        config = await ForcedCutsceneSoundsConfig.Load();

        configWindow = new ConfigAddon {
            Size = new Vector2(330.0f, 385.0f),
            InternalName = "ForcedCutsceneConfig",
            Title = Strings.ForcedCutsceneSounds_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.ForcedCutsceneSounds_CategoryGeneral)
            .AddCheckbox(Strings.ForcedCutsceneSounds_RestoreMuteState, nameof(config.Restore));

        configWindow.AddCategory(Strings.Toggles)
            .AddCheckbox(Strings.ForcedCutsceneSounds_UnmuteMaster, nameof(config.HandleMaster))
            .AddCheckbox(Strings.ForcedCutsceneSounds_UnmuteBgm, nameof(config.HandleBgm))
            .AddCheckbox(Strings.ForcedCutsceneSounds_UnmuteSe, nameof(config.HandleSe))
            .AddCheckbox(Strings.ForcedCutsceneSounds_UnmuteVoice, nameof(config.HandleVoice))
            .AddCheckbox(Strings.ForcedCutsceneSounds_UnmuteEnv, nameof(config.HandleEnv))
            .AddCheckbox(Strings.ForcedCutsceneSounds_UnmuteSystem, nameof(config.HandleSystem))
            .AddCheckbox(Strings.ForcedCutsceneSounds_UnmutePerform, nameof(config.HandlePerform));

        configWindow.AddCategory(Strings.ForcedCutsceneSounds_CategorySpecial)
            .AddCheckbox(Strings.ForcedCutsceneSounds_DisableMsq, nameof(config.DisableInMsqRoulette));

        OpenConfigAction = configWindow.Toggle;
    }

    public override async Task OnDisableAsync() {
        await IFramework.Get().DisposeMainThreaded(addTaskHook);
        addTaskHook = null;

        await Task.WhenAllDisposed(configWindow);
        configWindow = null;
        config = null;
        wasMuted = null;
    }

    private unsafe void OnTaskAdded(EventSceneModuleTaskManager* thisPtr, EventSceneTaskInterface* task) {
        try
        {
            addTaskHook!.Original(thisPtr, task);

            IPluginLog.Get().Debug($"SceneTaskAdded, Type: {task->Type} with flags {task->Flags}");

            if (config is null) return;
            if (config.DisableInMsqRoulette && AgentContentsFinder.Instance()->SelectedDuty is { ContentType: ContentsType.Roulette, Id: 3 }) return;

            switch (task->Type) {
                case EventSceneTaskType.PrepareCutScene:
                    MuteSounds();
                    break;

                case EventSceneTaskType.PostCutScene when config.Restore:
                    UnmuteSounds();
                    break;
            }

        }
        catch (Exception e)
        {
            IPluginLog.Get().Exception(e);
        }
    }

    private void MuteSounds() {
        if (wasMuted is null) return;

        foreach (var optionName in ConfigOptions) {
            var isMuted = IGameConfig.Get().System.TryGet(optionName, out bool value) && value;

            wasMuted[optionName] = isMuted;

            if (!ShouldHandle(optionName)) continue;
            if (!isMuted) continue;

            IGameConfig.Get().System.Set(optionName, false);
        }
    }

    private void UnmuteSounds() {
        if (wasMuted is null) return;

        foreach (var optionName in ConfigOptions) {
            if (!ShouldHandle(optionName)) continue;
            if (!wasMuted.TryGetValue(optionName, out var previousMuteValue)) continue;
            if (!previousMuteValue) continue;

            IGameConfig.Get().System.Set(optionName, previousMuteValue);
        }
    }

    private bool ShouldHandle(string optionName) {
        if (config is null) return false;

        return optionName switch {
            "IsSndMaster" => config.HandleMaster,
            "IsSndBgm" => config.HandleBgm,
            "IsSndSe" => config.HandleSe,
            "IsSndVoice" => config.HandleVoice,
            "IsSndEnv" => config.HandleEnv,
            "IsSndSystem" => config.HandleSystem,
            "IsSndPerform" => config.HandlePerform,
            _ => false,
        };
    }
}
