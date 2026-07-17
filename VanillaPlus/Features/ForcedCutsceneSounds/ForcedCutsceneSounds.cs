using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler;
using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Base;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Native.Addons;

namespace VanillaPlus.Features.ForcedCutsceneSounds;

public class ForcedCutsceneSounds : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ForcedCutsceneSounds,
        Description = Strings.ModificationDescription_ForcedCutsceneSounds,
        Authors = ["Haselnussbomber"],
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

    private unsafe delegate CutSceneController* CutSceneControllerDtorDelegate(CutSceneController* self, byte freeFlags);

    private Hook<ScheduleManagement.Delegates.CreateCutSceneController>? createCutSceneControllerHook;
    private Hook<CutSceneControllerDtorDelegate>? cutSceneControllerDtorHook;

    private ForcedCutsceneSoundsConfig? config;
    private ConfigAddon? configWindow;

    public override async Task OnEnableAsync() {
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

        unsafe {
            createCutSceneControllerHook = Services.GetService<IGameInteropProvider>().HookFromAddress<ScheduleManagement.Delegates.CreateCutSceneController>(
                ScheduleManagement.MemberFunctionPointers.CreateCutSceneController,
                CreateCutSceneControllerDetour);
            createCutSceneControllerHook.Enable();

            cutSceneControllerDtorHook = Services.GetService<IGameInteropProvider>().HookFromVTable<CutSceneControllerDtorDelegate>(
                CutSceneController.StaticVirtualTablePointer, 0,
                CutSceneControllerDtorDetour);
            cutSceneControllerDtorHook.Enable();
        }
    }

    public override async Task OnDisableAsync() {
        createCutSceneControllerHook?.Dispose();
        createCutSceneControllerHook = null;

        cutSceneControllerDtorHook?.Dispose();
        cutSceneControllerDtorHook = null;

        await Task.WhenAll(configWindow?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        configWindow = null;

        config = null;

        wasMuted = null;
    }

    private unsafe CutSceneController* CreateCutSceneControllerDetour(ScheduleManagement* thisPtr, byte* path, uint id, byte a4) {
        var result = createCutSceneControllerHook!.Original(thisPtr, path, id, a4);

        try {
            if (config is null) return result;
            if (config.DisableInMsqRoulette && AgentContentsFinder.Instance()->SelectedDuty is { ContentType: ContentsType.Roulette, Id: 3 }) return result;
            if (wasMuted is null || id is 0) return result;

            foreach (var optionName in ConfigOptions) {
                var isMuted = Services.GetService<IGameConfig>().System.TryGet(optionName, out bool value) && value;

                wasMuted[optionName] = isMuted;

                if (ShouldHandle(optionName) && isMuted) {
                    Services.GetService<IGameConfig>().System.Set(optionName, false);
                }
            }

        }
        catch (Exception e) {
            Services.PluginLog.Exception(e);
        }

        return result;
    }

    private unsafe CutSceneController* CutSceneControllerDtorDetour(CutSceneController* self, byte freeFlags) {
        try {
            if (config is null) {
                return cutSceneControllerDtorHook!.Original(self, freeFlags);
            }

            var cutsceneId = self->CutsceneId;

            if (config.Restore && cutsceneId is not 0) { // ignore title screen cutscene
                foreach (var optionName in ConfigOptions) {
                    if (ShouldHandle(optionName) && (wasMuted?.TryGetValue(optionName, out var value) ?? false) && value) {
                        Services.GetService<IGameConfig>().System.Set(optionName, value);
                    }
                }
            }
        }
        catch (Exception e) {
            Services.PluginLog.Exception(e);
        }

        return cutSceneControllerDtorHook!.Original(self, freeFlags);
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
