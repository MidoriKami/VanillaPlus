using System;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.MacroTooltips;

/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public unsafe class MacroTooltips : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_MacroTooltips,
        Description = Strings.ModificationDescription_MacroTooltips,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    private Hook<AddonActionBarBase.Delegates.ShowTooltip>? showTooltipHook;

    public override string ImageName => "MacroTooltips.png";

    public override Task OnEnableAsync() {
        showTooltipHook = IGameInteropProvider.Get().HookFromAddress<AddonActionBarBase.Delegates.ShowTooltip>(AddonActionBarBase.MemberFunctionPointers.ShowTooltip, OnShowMacroTooltip);
        showTooltipHook?.Enable();

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        showTooltipHook?.Dispose();
        showTooltipHook = null;

        return Task.CompletedTask;
    }

    private void OnShowMacroTooltip(AddonActionBarBase* a1, AtkResNode* macroResNode, NumberArrayData* numberArray, StringArrayData* stringArray, int numberArrayIndex, int stringArrayIndex) {
        try {
            // In ActionBarNumberArray, the first hotbar starts at index 15
            var realSlotId = (numberArrayIndex - 15) % 16;
            var realHotbarId = (numberArrayIndex - 15) / 272;
            var originalTooltip = stringArray->StringArray[stringArrayIndex];

            // When using a shared pet/accessory hotbar, the hotbar id will be out of range
            // These slots can't have macros, so we will ignore them entirely
            if (realHotbarId >= RaptureHotbarModule.Instance()->Hotbars.Length) {
                showTooltipHook!.Original(a1, macroResNode, numberArray, stringArray, numberArrayIndex, stringArrayIndex);
                return;
            }
            var hotbarSlot = RaptureHotbarModule.Instance()->Hotbars[realHotbarId].Slots[realSlotId];

            if (hotbarSlot is { CommandType: RaptureHotbarModule.HotbarSlotType.Macro, ApparentSlotType: RaptureHotbarModule.HotbarSlotType.Action }) {
                macroResNode->ShowActionTooltip(hotbarSlot.ApparentActionId, originalTooltip.ToString());
                return;
            }
        }
        catch (Exception e) {
            IPluginLog.Get().Exception(e);
        }

        showTooltipHook!.Original(a1, macroResNode, numberArray, stringArray, numberArrayIndex, stringArrayIndex);
    }
}
