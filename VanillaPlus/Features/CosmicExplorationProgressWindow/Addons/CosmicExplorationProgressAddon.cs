using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.Features.CosmicExplorationProgressWindow.Classes;
using VanillaPlus.Features.CosmicExplorationProgressWindow.Nodes;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow.Addons;

public class CosmicExplorationProgressAddon : NativeAddon {
    private ListNode<Progress, WksProgressListItemNode>? listNode;
    private bool watchHud;
    private uint? lastClassJob;

    private List<Progress>? allOptions;
    
    public CosmicExplorationProgressAddon() {
        CreateWindowNode = () => new WksWindowNode();
        ContentPadding = Vector2.Zero;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        allOptions = GetInitialProgressList();
        
        listNode = new ListNode<Progress, WksProgressListItemNode> {
            Position = ContentStartPosition,
            Size = ContentSize,
            OptionsList = allOptions.Where(entry => entry.JobId == Services.PlayerState.ClassJob.Value.RowId - 7).ToList(),
            ItemSpacing = 1.0f,
        };
        listNode.ScrollBarNode.HideWhenDisabled = true;
        listNode.ScrollBarNode.IsEnabled = false;
        listNode.ScrollBarNode.IsVisible = false;
        listNode.AttachNode(this);

        addon->Flags1C8 = 0x100001; // Properly allow ESC-closing.
        watchHud = true;
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        lastClassJob = null;

        allOptions?.Clear();
        allOptions = null;
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        if (listNode is null) return;
        if (allOptions is null) return;
        if (!watchHud) return; 
        
        // We want to hide with the normal WKS HUD,
        // Hiding custom addons is generally disallowed unless special care is taken to ensure the addon is not too long-lived.
        var hud = RaptureAtkUnitManager.Instance()->GetAddonByName("WKSHud");
        if (hud is null) return;

        addon->IsVisible = hud->IsVisible;

        if (!addon->IsVisible) return;

        var research = WKSManager.Instance()->ResearchModule;
        if (research is null) return;

        if (lastClassJob != Services.PlayerState.ClassJob.Value.RowId) {
            lastClassJob = Services.PlayerState.ClassJob.Value.RowId;
            listNode.OptionsList = allOptions.Where(entry => entry.JobId == lastClassJob - 7).ToList();
        }

        foreach (var progress in listNode.OptionsList) {
            progress.Current = research->GetCurrentAnalysis(progress.JobId, progress.ResearchType);
            progress.Needed = research->GetNeededAnalysis(progress.JobId, progress.ResearchType);
            progress.Max = research->GetMaxAnalysis(progress.JobId, progress.ResearchType);
        }

        listNode.Update();
    }

    protected override unsafe void OnHide(AtkUnitBase* addon)
        => watchHud = false;

    private static List<Progress> GetInitialProgressList() {
        List<Progress> researchProgress = [];

        var toolSheet = Services.DataManager.Excel.GetSheet<WKSCosmoToolClass>();
        foreach (var toolClassRow in toolSheet.Where(row => row.RowId is not 0)) {
            var jobId = (byte)toolClassRow.RowId;
            
            foreach (var index in Enumerable.Range(0, toolClassRow.Types.Count)) {
                researchProgress.Add(new Progress(jobId, (byte)(index + 1)) {
                    IconId = toolClassRow.Types[index].Icon,
                    IconTooltip = toolClassRow.Types[index].Name.ValueNullable?.Name ?? string.Empty,
                });
            }
        }

        return researchProgress;
    }
}
