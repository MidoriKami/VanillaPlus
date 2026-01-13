using System.Linq;
using System.Numerics;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.ActionHighlight.Nodes;

public class ActionHighlightConfigNode : ConfigNode<ActionCategory> {
    private readonly ScrollingListNode actionsList;
    private readonly ScrollingListNode generalSettingsArea;
    private ActionHighlightConfig? config;

    public ActionHighlightConfigNode() {
        actionsList = new ScrollingListNode {
            AutoHideScrollBar = true, 
            FitContents = true,
        };
        actionsList.AttachNode(this);

        generalSettingsArea = new ScrollingListNode {
            AutoHideScrollBar = true, 
            IsVisible = false, 
            FitContents = true,
        };
        generalSettingsArea.AttachNode(this);
    }

    public void SetConfig(ActionHighlightConfig highlightConfig) 
        => config = highlightConfig;

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        actionsList.Size = Size;
        generalSettingsArea.Size = Size;

        actionsList.RecalculateLayout();
        generalSettingsArea.RecalculateLayout();
    }

    protected override void OptionChanged(ActionCategory? option) {
        if (option is null || config is null) {
            actionsList.IsVisible = generalSettingsArea.IsVisible = false;
            return;
        }

        if (option.Type is ActionCategoryType.General) {
            actionsList.IsVisible = false;
            generalSettingsArea.IsVisible = true;

            if (generalSettingsArea.Nodes.Count is 0) {
                generalSettingsArea.AddNode(new GeneralSettingsNode(config) {
                    Size = new Vector2(generalSettingsArea.Width - 32.0f, 200.0f),
                    Position = new Vector2(16.0f, 0.0f),
                });
            }
            generalSettingsArea.RecalculateLayout();
            return;
        }

        generalSettingsArea.IsVisible = false;
        actionsList.IsVisible = true;

        var job = option.Job.GetValueOrDefault();

        var actions = option.Type switch {
            ActionCategoryType.Role => Services.DataManager.RoleActions.ToList(),
            ActionCategoryType.Job  => ActionHighlight.GetClassActions().Where(action => IsValidAction(job, action)).OrderBy(a => a.ClassJobLevel).ToList(),
            _ => [],
        };

        actionsList.SyncWithListData(actions, node => node.Action, action => new ActionSettingNode(config, action) {
            Size = new Vector2(actionsList.ContentWidth, 40.0f),
        });

        actionsList.RecalculateLayout();
    }
    
    private static bool IsValidAction(ClassJob job, Action action) {
        if (action.IsUsableByJob(job)) return true;

        var whitelisted = ActionHighlight.JobActionWhiteList.TryGetValue(job.RowId, out var list);
        return whitelisted && list != null && list.Contains((int)action.RowId);
    }
}
