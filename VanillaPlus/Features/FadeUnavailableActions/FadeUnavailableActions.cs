using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Extensions;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using Action = Lumina.Excel.Sheets.Action;
using ActionBarSlotNumberArray = FFXIVClientStructs.FFXIV.Client.UI.Arrays.ActionBarNumberArray.ActionBarBarNumberArray.ActionBarSlotNumberArray;

namespace VanillaPlus.Features.FadeUnavailableActions;

public unsafe class FadeUnavailableActions : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Fade Unavailable Actions",
        Description = "Fades hotbar slots when the action is not able to be cast due to missing resources, out of range, or just on cooldown.\n\n" +
                      "Additionally fades actions that are not available because you are sync'd down.",
        Authors = ["MidoriKami"],
        Type = ModificationType.UserInterface,
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
        CompatabilityModule = new SimpleTweaksCompatabilityModule("UiAdjustments@FadeUnavailableActions"),
    };

    private delegate void UpdateHotBarSlotDelegate(AddonActionBarBase* addon, ActionBarSlot* uiData, NumberArrayData* numberArray, StringArrayData* stringArray, int numberArrayIndex, int stringArrayIndex);

    [Signature("E8 ?? ?? ?? ?? 48 81 C6 ?? ?? ?? ?? 83 C7 11", DetourName = nameof(OnHotBarSlotUpdate))]
    private Hook<UpdateHotBarSlotDelegate>? onHotBarSlotUpdateHook;

    private Dictionary<uint, Action?>? actionCache;

    private FadeUnavailableActionsConfig? config;
    private FadeUnavailableActionsConfigWindow? configWindow;

    public override string ImageName => "FadeUnavailableActions.png";

    public override void OnEnable() {
        actionCache = [];
        
        config = FadeUnavailableActionsConfig.Load();
        configWindow = new FadeUnavailableActionsConfigWindow(config);
        configWindow.AddToWindowSystem();
        OpenConfigAction = configWindow.Toggle;
        
        Services.Hooker.InitializeFromAttributes(this);
        onHotBarSlotUpdateHook?.Enable();
    }

    public override void OnDisable() {
        onHotBarSlotUpdateHook?.Dispose();
        onHotBarSlotUpdateHook = null;
        
        configWindow?.RemoveFromWindowSystem();
        configWindow = null;

        actionCache = null;
        
        ResetAllHotbars();
    }
    
    private void OnHotBarSlotUpdate(AddonActionBarBase* addon, ActionBarSlot* hotBarSlotData, NumberArrayData* numberArray, StringArrayData* stringArray, int numberArrayIndex, int stringArrayIndex) {
        try {
            ProcessHotBarSlot(hotBarSlotData, numberArray, numberArrayIndex);
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Something went wrong in FadeUnavailableActions, let MidoriKami know!");
        } finally {
            onHotBarSlotUpdateHook!.Original(addon, hotBarSlotData, numberArray, stringArray, numberArrayIndex, stringArrayIndex);
        }
    }
    
    private void ProcessHotBarSlot(ActionBarSlot* hotBarSlotData, NumberArrayData* numberArray, int numberArrayIndex) {
        if (config is null) return;
        if (Services.ClientState.LocalPlayer is { IsCasting: true } ) return;

        var numberArrayData = (ActionBarSlotNumberArray*) (&numberArray->IntArray[numberArrayIndex]);

        if ((NumberArrayActionType)numberArrayData->ActionType is not (NumberArrayActionType.Action or NumberArrayActionType.CraftAction)) {
            ApplyColoring(hotBarSlotData, false, false);
            return;
        }

        if (config.ApplyToSyncActions) {
            var action = GetAction(numberArrayData->ActionId);

            var actionLevel = action?.ClassJobLevel ?? 0;
            var playerLevel = Services.ClientState.LocalPlayer?.Level ?? 0;

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

    private Action? GetAction(uint actionId) {
        var adjustedActionId = ActionManager.Instance()->GetAdjustedActionId(actionId);

        if (actionCache?.TryGetValue(adjustedActionId, out var action) ?? false) return action;

        action = Services.DataManager.GetExcelSheet<Action>().GetRowOrDefault(adjustedActionId);
        actionCache?.Add(adjustedActionId, action);
        return action;
    }

    private bool ShouldFadeAction(ActionBarSlotNumberArray* numberArrayData) 
        => !(numberArrayData->Executable && numberArrayData->Executable2);

    private void ApplyColoring(ActionBarSlot* hotBarSlotData, bool redden, bool fade) {
        if (config is null) return;
        if (hotBarSlotData is null) return;

        var icon = hotBarSlotData->GetImageNode();
        var frame = hotBarSlotData->GetFrameNode();
        
        if (icon is null || frame is null) return;

        icon->Color.R = 0xFF;
        icon->Color.G = config.ReddenOutOfRange && redden ? (byte)(0xFF * ((100 - config.ReddenPercentage) / 100.0f)) : (byte)0xFF;
        icon->Color.B = config.ReddenOutOfRange && redden ? (byte)(0xFF * ((100 - config.ReddenPercentage) / 100.0f)) : (byte)0xFF;
        icon->Color.A = fade ? (byte)(0xFF * ((100 - config.FadePercentage) / 100.0f)) : (byte)0xFF;

        frame->Color.A = fade ? config.ApplyToFrame ? (byte)(0xFF * ((100 - config.FadePercentage) / 100.0f)) : (byte) 0xFF : (byte)0xFF;
    }

    private void ResetAllHotbars() {
        foreach (var addon in RaptureAtkUnitManager.Instance()->AllLoadedUnitsList.Entries) {
            if (addon.Value is null) continue;
            if (addon.Value->NameString.Contains("_Action") && !addon.Value->NameString.Contains("Contents")) {
                var actionBar = (AddonActionBarBase*)addon.Value;
                if (actionBar is null) continue;
                if (actionBar->ActionBarSlotVector.First is null) continue;

                foreach (var slot in actionBar->ActionBarSlotVector) {
                    if (slot.Icon is not null) {
                        var iconComponent = (AtkComponentIcon*) slot.Icon->Component;
                        if (iconComponent is null) continue;

                        iconComponent->IconImage->Color = Vector4.One.ToByteColor();
                        iconComponent->Frame->Color = Vector4.One.ToByteColor();
                    }
                }
            }
        }
    }

    private enum NumberArrayActionType : uint {
        Action = 0x2E,
        CraftAction = 0x36,
    }
}
