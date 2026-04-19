using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Config;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace VanillaPlus.Features.MSQProgressPercent;

public unsafe class MSQProgressBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.MSQProgressBar_DisplayName,
        Description = Strings.MSQProgressBar_Description,
        Type = ModificationType.UserInterface,
        Authors = [ "MidoriKami" ],
    };

    public override string ImageName => "MSQProgressBar.png";

    private AddonController? scenarioTreeAddonController;
    private ProgressBarNode? progressBarNode;

    private Dictionary<ExVersion, Range>? expansionRanges;
    private MSQProgressBarConfig? config;
    private ConfigAddon? configAddon;
    
    public override void OnEnable() {
        config = MSQProgressBarConfig.Load();
        
        expansionRanges = [];

        foreach (var expansion in Services.DataManager.GetExcelSheet<ExVersion>()) {
            var scenarioTreesForExpansion = Services.DataManager.GetExcelSheet<ScenarioTree>()
                .Where(scenarioTree => Services.DataManager.GetExcelSheet<Quest>().GetRowOrDefault(scenarioTree.RowId)?.Expansion.RowId == expansion.RowId)
                .ToList();

            var min = scenarioTreesForExpansion.Min(entry => entry.Unknown2);
            var max = scenarioTreesForExpansion.Max(entry => entry.Unknown2);
            
            expansionRanges.TryAdd(expansion, min..max);
            
            Services.PluginLog.Debug($"Range for {expansion.Name}: {min}..{max}");
        }

        configAddon = new ConfigAddon {
            Config = config,
            InternalName = "MSQProgressBarConfig",
            Title = Strings.MSQProgressBar_ConfigTitle,
        };

        configAddon.AddCategory(Strings.MSQProgressBar_CategoryGeneral)
            .AddDropdown(Strings.MSQProgressBar_LabelMode, nameof(config.Mode), new Dictionary<string, object> {
                [Strings.MSQProgressBar_ModeEntireGame] = MSQProgressBarMode.EntireGame,
                [Strings.MSQProgressBar_ModeExpansion] = MSQProgressBarMode.Expansion,
            })
            .AddColorEdit(Strings.MSQProgressBar_LabelBarColor, nameof(config.BarColor), KnownColor.White.Vector());

        OpenConfigAction = configAddon.Toggle;
        
        scenarioTreeAddonController = new AddonController {
            AddonName = "ScenarioTree",
            OnSetup = addon => {
                var targetPositioningNode = addon->GetNodeById<AtkComponentNode>(13);
                var msqTextNode = targetPositioningNode->SearchNodeById<AtkTextNode>(6);
            
                progressBarNode = new ProgressBarNode {
                    Size = new Vector2(msqTextNode->Width, 9.0f),
                    Position = new Vector2(0.0f, msqTextNode->Height - 3.0f),
                    TextTooltip = string.Format(Strings.MSQProgressBar_TooltipCurrentProgress, "000"),
                    Progress = 0.5f,
                };
                progressBarNode.AttachNode(msqTextNode, NodePosition.BeforeTarget);
            
                UpdateProgress(addon);
            },
            OnUpdate = addon => {
                if (addon->AtkValuesCount < 7) return;
            
                UpdateProgress(addon);
                progressBarNode?.BarColor = config.BarColor with { W = 1.0f };
                progressBarNode?.Alpha = config.BarColor.W;
            },
            OnFinalize = _ => {
                progressBarNode?.Dispose();
                progressBarNode = null;
            },
        };
        scenarioTreeAddonController.Enable();
    }

    public override void OnDisable() {
        scenarioTreeAddonController?.Dispose();
        scenarioTreeAddonController = null;

        expansionRanges?.Clear();
        expansionRanges = null;
        
        configAddon?.Dispose();
        configAddon = null;
        
        config = null;
    }

    private void UpdateProgress(AtkUnitBase* addon) {
        if (addon->AtkValuesCount < 7) return;
        if (expansionRanges is null) return;

        if (addon->AtkValuesSpan[6].Type is not ValueType.String) {
            progressBarNode?.Progress = 1.0f;
            progressBarNode?.TextTooltip = Strings.MSQProgressBar_TooltipGameComplete;
            return;
        }

        var currentQuest = int.Parse(addon->AtkValuesSpan[6].String);
        var expansionRange = expansionRanges.FirstOrNull(pair => pair.Value.Contains(currentQuest));
        if (expansionRange is not { Value: var range }) return;

        switch (config?.Mode) {
            case MSQProgressBarMode.EntireGame:
                var minTreeEntry = expansionRanges.Values.Min(expansion => expansion.Start.Value);
                var maxTreeEntry = expansionRanges.Values.Max(expansion => expansion.End.Value);
                var length = maxTreeEntry - minTreeEntry;
                
                progressBarNode?.Progress = (float)(currentQuest - minTreeEntry) / length;
                progressBarNode?.TextTooltip = string.Format(Strings.MSQProgressBar_TooltipGameProgress, progressBarNode.Progress * 100.0f);
                break;
            
            case MSQProgressBarMode.Expansion:
                progressBarNode?.Progress = (float)(currentQuest - range.Start.Value) / range.Length;
                progressBarNode?.TextTooltip = string.Format(Strings.MSQProgressBar_TooltipExpansionProgress, progressBarNode.Progress * 100.0f);
                break;
        }
    }
}
