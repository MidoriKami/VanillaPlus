using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.ActionHighlight;

public class ActionHighlightConfigNode : ConfigNode<ClassJobWrapper> {
    private readonly ScrollingListNode actionsList;
    private readonly ScrollingListNode? generalSettingsArea;
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

    public void SetConfig(ActionHighlightConfig highlightConfig) {
        config = highlightConfig;
    }

    protected override void OptionChanged(ClassJobWrapper? option) {
        if (option is null || config is null) {
            actionsList.IsVisible = false;
            generalSettingsArea?.IsVisible = false;
            return;
        }

        if (option.IsGeneralSettings) {
            actionsList.IsVisible = false;
            if (generalSettingsArea != null) {
                generalSettingsArea.IsVisible = true;

                if (generalSettingsArea.Nodes.Count == 0) {
                    var settingsNode = new GeneralSettingsNode(config) {
                        Size = new Vector2(generalSettingsArea.Width - 32.0f, 200.0f),
                        Position = new Vector2(16.0f, 0.0f),
                    };
                    generalSettingsArea.AddNode(settingsNode);
                    generalSettingsArea.RecalculateLayout();
                }
            }
            return;
        }

        generalSettingsArea?.IsVisible = false;
        actionsList.IsVisible = true;

        List<Action> actions;
        if (option.IsRoleActions) {
            actions = Services.DataManager.RoleActions.ToList();
        } else {
            actions = ActionHighlight.GetClassActions()
                .Where(a => a.ClassJob.RowId == option.ClassJob!.Value.RowId ||
                            a.ClassJob.RowId == option.ClassJob.Value.ClassJobParent.RowId ||
                            ActionHighlight.JobActionWhiteList.TryGetValue(option.ClassJob!.Value.RowId, out var whiteList) && whiteList.Contains((int)a.RowId))
                .OrderBy(a => a.ClassJobLevel)
                .ToList();
        }

        actionsList.SyncWithListData(actions, node => node.Action, action => new ActionSettingNode(config, action) {
            Size = new Vector2(actionsList.ContentWidth, 40.0f),
        });

        actionsList.RecalculateLayout();
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        generalSettingsArea?.Size = Size;
        actionsList.Size = Size;
        
        generalSettingsArea?.RecalculateLayout();
        actionsList.RecalculateLayout();
    }
}
