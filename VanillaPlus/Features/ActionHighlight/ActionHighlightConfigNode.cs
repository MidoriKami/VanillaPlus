using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.ActionHighlight;

public class ActionHighlightConfigNode : ConfigNode<ClassJobWrapper> {
    private readonly ScrollingAreaNode<VerticalListNode> actionsList;
    private readonly ScrollingAreaNode<VerticalListNode>? generalSettingsArea;
    private ActionHighlightConfig? config;

    public ActionHighlightConfigNode() {
        actionsList = new ScrollingAreaNode<VerticalListNode> {
            ContentHeight = 100.0f,
            AutoHideScrollBar = true,
        };
        actionsList.ContentNode.FitContents = true;
        actionsList.AttachNode(this);

        generalSettingsArea = new ScrollingAreaNode<VerticalListNode> {
            ContentHeight = 100.0f,
            AutoHideScrollBar = true,
            IsVisible = false,
        };
        generalSettingsArea.ContentNode.FitContents = true;
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

                if (generalSettingsArea.ContentNode.Nodes.Count == 0) {
                    var settingsNode = new GeneralSettingsNode(config) {
                        Size = new Vector2(generalSettingsArea.ContentNode.Width, 200.0f)
                    };
                    generalSettingsArea.ContentNode.AddNode(settingsNode);
                    generalSettingsArea.ContentNode.RecalculateLayout();
                    generalSettingsArea.ContentHeight = generalSettingsArea.ContentNode.Height;
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
                            (ActionHighlight.JobActionWhiteList.TryGetValue(option.ClassJob!.Value.RowId, out var whiteList) && whiteList.Contains((int)a.RowId)))
                .OrderBy(a => a.ClassJobLevel)
                .ToList();
        }

        actionsList.ContentNode.SyncWithListData(actions, node => node.Action, action => new ActionSettingNode(config, action) {
            Size = new Vector2(actionsList.ContentNode.Width, 40.0f),
        });

        actionsList.ContentNode.RecalculateLayout();
        actionsList.ContentHeight = actionsList.ContentNode.Height;
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        actionsList.Size = Size;
        actionsList.ContentNode.Width = Width;

        if (generalSettingsArea != null) {
            generalSettingsArea.Size = Size;
            generalSettingsArea.ContentNode.Width = Width;
            foreach (var node in generalSettingsArea.ContentNode.GetNodes<GeneralSettingsNode>()) {
                node.Size = new Vector2(Width, node.Height);
            }
        }

        foreach (var node in actionsList.ContentNode.GetNodes<ActionSettingNode>()) {
            node.Size = new Vector2(Width, 40.0f);
        }
    }
}

