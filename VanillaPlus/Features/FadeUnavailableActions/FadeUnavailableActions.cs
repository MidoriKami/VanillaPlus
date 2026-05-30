using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays.Common;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;
using Action = Lumina.Excel.Sheets.Action;

namespace VanillaPlus.Features.FadeUnavailableActions;

public class FadeUnavailableActions : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FadeUnavailableActions,
        Description = Strings.ModificationDescription_FadeUnavailableActions,
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        CompatibilityModule = new SimpleTweaksCompatibilityModule("UiAdjustments@FadeUnavailableActions", "1.14.0.2"),
    };

    private Hook<AddonActionBarBase.Delegates.UpdateHotbarSlot>? onHotBarSlotUpdateHook;

    private Dictionary<uint, Action?>? actionCache;

    private FadeUnavailableActionsConfig? config;
    private ConfigAddon? configWindow;

    public override string ImageName => "FadeUnavailableActions.png";

    public override async Task OnEnableAsync() {
        actionCache = [];

        config = await FadeUnavailableActionsConfig.Load();

        configWindow = new ConfigAddon {
            Size = new Vector2(400.0f, 250.0f),
            InternalName = "FadeUnavailableConfig",
            Title = Strings.FadeUnavailableActions_ConfigTitle,
            Config = config,
        };

        configWindow.AddCategory(Strings.FadeUnavailableActions_CategoryStyleSettings)
            .AddIntSlider(Strings.FadeUnavailableActions_LabelFadePercentage, 0, 90, nameof(config.FadePercentage))
            .AddIntSlider(Strings.FadeUnavailableActions_LabelReddenPercentage, 5, 100, nameof(config.ReddenPercentage));

        configWindow.AddCategory(Strings.FadeUnavailableActions_CategoryFeatureToggles)
            .AddCheckbox(Strings.FadeUnavailableActions_LabelApplyToFrame, nameof(config.ApplyToFrame))
            .AddCheckbox(Strings.FadeUnavailableActions_LabelApplyToSync, nameof(config.ApplyToSyncActions))
            .AddCheckbox(Strings.FadeUnavailableActions_LabelReddenOutOfRange, nameof(config.ReddenOutOfRange));

        OpenConfigAction = configWindow.Toggle;

        unsafe {
            onHotBarSlotUpdateHook = Services.Hooker.HookFromAddress<AddonActionBarBase.Delegates.UpdateHotbarSlot>(AddonActionBarBase.MemberFunctionPointers.UpdateHotbarSlot, OnHotBarSlotUpdate);
            onHotBarSlotUpdateHook?.Enable();
        }
    }

    public override async Task OnDisableAsync() {
        onHotBarSlotUpdateHook?.Dispose();
        onHotBarSlotUpdateHook = null;

        await Task.WhenAll(configWindow?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        configWindow = null;

        actionCache = null;

        await Services.Framework.Run(ResetAllHotbars);
    }

    private unsafe void OnHotBarSlotUpdate(AddonActionBarBase* addon, ActionBarSlot* hotBarSlotData, NumberArrayData* numberArray, StringArrayData* stringArray, int numberArrayIndex, int stringArrayIndex) {
        try {
            ProcessHotBarSlot(hotBarSlotData, numberArray, numberArrayIndex);
        }
        catch (Exception e) {
            Services.PluginLog.Exception(e);
        } finally {
            onHotBarSlotUpdateHook!.Original(addon, hotBarSlotData, numberArray, stringArray, numberArrayIndex, stringArrayIndex);
        }
    }

    private unsafe void ProcessHotBarSlot(ActionBarSlot* hotBarSlotData, NumberArrayData* numberArray, int numberArrayIndex) {
        if (config is null) return;
        if (Services.ObjectTable.LocalPlayer is { IsCasting: true }) return;

        var numberArrayData = (ActionBarSlotNumberArray*)(&numberArray->IntArray[numberArrayIndex]);

        if ((NumberArrayActionType)numberArrayData->ActionType is not (NumberArrayActionType.Action or NumberArrayActionType.CraftAction)) {
            ApplyColoring(hotBarSlotData, false, false);
            return;
        }

        if (config.ApplyToSyncActions) {
            var action = GetAction(numberArrayData->ActionId);

            var actionLevel = action?.ClassJobLevel ?? 0;
            var playerLevel = Services.ObjectTable.LocalPlayer?.Level ?? 0;

            switch (action) {
                case null:
                    ApplyColoring(hotBarSlotData, false, false);
                    break;

                case { IsRoleAction: false } when actionLevel > playerLevel:
                    ApplyColoring(hotBarSlotData, false, true);
                    break;

                default:
                    ApplyColoring(hotBarSlotData, false, false);
                    break;
            }
        }
        else {
            ApplyColoring(hotBarSlotData, !numberArrayData->InRange, ShouldFadeAction(numberArrayData));
        }
    }

    private unsafe Action? GetAction(uint actionId) {
        var adjustedActionId = ActionManager.Instance()->GetAdjustedActionId(actionId);

        if (actionCache?.TryGetValue(adjustedActionId, out var action) ?? false) return action;

        action = Services.DataManager.GetExcelSheet<Action>().GetRowOrDefault(adjustedActionId);
        actionCache?.Add(adjustedActionId, action);
        return action;
    }

    private static unsafe bool ShouldFadeAction(ActionBarSlotNumberArray* numberArrayData)
        => !(numberArrayData->Executable && numberArrayData->Executable2);

    private unsafe void ApplyColoring(ActionBarSlot* hotBarSlotData, bool redden, bool fade) {
        if (config is null) return;
        if (hotBarSlotData is null) return;

        var icon = hotBarSlotData->ImageNode;
        var frame = hotBarSlotData->FrameNode;

        if (icon is null || frame is null) return;

        icon->Color.R = 0xFF;
        icon->Color.G = config.ReddenOutOfRange && redden ? (byte)(0xFF * ((100 - config.ReddenPercentage) / 100.0f)) : (byte)0xFF;
        icon->Color.B = config.ReddenOutOfRange && redden ? (byte)(0xFF * ((100 - config.ReddenPercentage) / 100.0f)) : (byte)0xFF;
        icon->Color.A = fade ? (byte)(0xFF * ((100 - config.FadePercentage) / 100.0f)) : (byte)0xFF;

        frame->Color.A = fade ? config.ApplyToFrame ? (byte)(0xFF * ((100 - config.FadePercentage) / 100.0f)) : (byte)0xFF : (byte)0xFF;
    }

    private static unsafe void ResetAllHotbars() {
        foreach (var addon in RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries) {
            if (addon.Value is null) continue;
            if (addon.Value->NameString.Contains("_Action") && !addon.Value->NameString.Contains("Contents")) {
                var actionBar = (AddonActionBarBase*)addon.Value;
                if (actionBar is null) continue;
                if (actionBar->ActionBarSlotVector.First is null) continue;

                foreach (var slot in actionBar->ActionBarSlotVector) {
                    if (slot.Icon is not null) {
                        var iconComponent = (AtkComponentIcon*)slot.Icon->Component;
                        if (iconComponent is null) continue;

                        iconComponent->IconImage->Color = Vector4.One.ToByteColor();
                        iconComponent->Frame->Color = Vector4.One.ToByteColor();
                    }
                }
            }
        }
    }

    private enum NumberArrayActionType : uint {
        Action = 0x2F,
        CraftAction = 0x37,
    }
}
