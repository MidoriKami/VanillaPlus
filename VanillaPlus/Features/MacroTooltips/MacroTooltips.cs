using System;
using System.Linq;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using Action = Lumina.Excel.Sheets.Action;

namespace VanillaPlus.Features.MacroTooltips;

/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public unsafe class MacroTooltips : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Macro Tooltips",
        Description = "Displays action tooltips when hovering over a macro with '/macroicon' set with an 'action'",
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private delegate void ShowMacroTooltipDelegate(AddonActionBarBase* a1, AtkResNode* a2, NumberArrayData* a3, StringArrayData* a4, int a5, int a6);
    
    [Signature("E8 ?? ?? ?? ?? 4C 8B 64 24 ?? 48 8B 7C 24 ?? 48 8B 74 24 ?? 4C 8B 6C 24 ??", DetourName = nameof(OnShowMacroTooltip))]
    private Hook<ShowMacroTooltipDelegate>? showTooltipHook;

    public override string ImageName => "MacroTooltips.png";

    public override void OnEnable() {
        Services.Hooker.InitializeFromAttributes(this);
        showTooltipHook?.Enable();
    }

    public override void OnDisable() {
        showTooltipHook?.Dispose();
        showTooltipHook = null;
    }

    private void OnShowMacroTooltip(AddonActionBarBase* a1, AtkResNode* a2, NumberArrayData* numberArray, StringArrayData* a4, int numberArrayIndex, int stringArrayIndex) {
        try {
            // In ActionBarNumberArray, the first hotbar starts at index 15
            var realSlotId = (numberArrayIndex - 15) % 16;
            var realHotbarId = (numberArrayIndex - 15) / 272;
            
            var hotbarSlot = RaptureHotbarModule.Instance()->Hotbars[realHotbarId].Slots[realSlotId];
            if (hotbarSlot is { CommandType: RaptureHotbarModule.HotbarSlotType.Macro, ApparentSlotType: RaptureHotbarModule.HotbarSlotType.Action } ) {
                ref var macro = ref GetMacroFromCommandId(hotbarSlot.CommandId);
                
                if (GetMacroCommandLine(ref macro) is { } iconCommandLine) {
                    var parts = iconCommandLine.Split(' ');
                    var actionNamePart = parts[1].Replace("\"", string.Empty);
                    
                    var matchingAction = Services.DataManager.GetExcelSheet<Action>().FirstOrDefault(action => action.Name == actionNamePart);
                    if (matchingAction is { RowId: not 0 }) {
                        var tooltipArgs = stackalloc AtkTooltipManager.AtkTooltipArgs[1];
                        tooltipArgs->ActionArgs = new AtkTooltipManager.AtkTooltipArgs.AtkTooltipActionArgs {
                            Id = (int) matchingAction.RowId,
                            Kind = DetailKind.Action,
                            Flags = 1,
                        };
                    
                        AtkStage.Instance()->TooltipManager.ShowTooltip(
                            AtkTooltipManager.AtkTooltipType.Action,
                            a1->Id,
                            a2,
                            tooltipArgs
                        );
                    
                        return;
                    }
                }
            }
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Exception in OnShowMacroTooltip");
        }

        showTooltipHook!.Original(a1, a2, numberArray, a4, numberArrayIndex, stringArrayIndex);
    }

    private static ref RaptureMacroModule.Macro GetMacroFromCommandId(uint commandId) {
        var macroModule = RaptureMacroModule.Instance();

        if (commandId >= 0x100) {
            return ref macroModule->Shared[(int)commandId - 0x100];
        }

        return ref macroModule->Individual[(int)commandId];
    }

    private static string? GetMacroCommandLine(ref RaptureMacroModule.Macro macro) {
        foreach (var line in macro.Lines) {
            var parsedLine = line.ToString();
            
            if (parsedLine.Contains("micon") || parsedLine.Contains("macroicon")) return parsedLine;
        }

        return null;
    }
}
