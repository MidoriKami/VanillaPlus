using System;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using KamiToolKit.Components.ConfigurationNodes;
using KamiToolKit.Nodes;
using Lumina.Data.Parsing.Uld;
using Lumina.Excel.Sheets;
using VanillaPlus.Features.ActionHighlight.Config;
using Action = Lumina.Excel.Sheets.Action;

namespace VanillaPlus.Features.ActionHighlight.Nodes;

/// <summary>
/// Node that represents the configuration of one <see cref="AntsClassJobConfig"/> with options to configure any of that jobs actions.
/// </summary>
public class AntsClassJobConfigurationNode : EntryConfigurationNode<AntsClassJobConfig> {

    /// <summary>
    /// Gets the ClassJobConfig for the currently selected ClassJob or null.
    /// </summary>
    public static AntsClassJobConfig? ClassJobConfig { get; private set; }

    /// <inheritdoc/>
    protected override void ClearEntryData() {
        base.ClearEntryData();

        ClassJobConfig = null;
    }

    /// <inheritdoc/>
    protected override void PopulateEntryData(AntsClassJobConfig entry) {
        ClassJobConfig = entry;
        var classJob = Services.GetService<IDataManager>().GetExcelSheet<ClassJob>().GetRow(entry.ClassJobId);

        backgroundImageNode.IconId = 62000 + entry.ClassJobId;

        actionsListNode.OptionsList = ActionHighlight.GetClassActions(classJob);

        rolesListNode.OptionsList = Services.GetService<IDataManager>().GetExcelSheet<Action>()
            .Where(action => action.ClassJobCategory.Value.ClassesJobs[(int) entry.ClassJobId])
            .Where(action => action is { IsRoleAction: true, IsPvP: false })
            .ToList();
    }

    public AntsClassJobConfigurationNode() {
        backgroundImageNode = new IconImageNode {
            FitTexture = true,
            Alpha = 0.1f,
        };
        backgroundImageNode.AttachNode(ConfigurationContentNode);

        actionsCategoryNode = new CategoryTextNode {
            TextId = 14723, // "Job Actions"
            SheetType = NodeData.SheetType.Addon,
        };
        actionsCategoryNode.AttachNode(ConfigurationContentNode);

        actionsListNode = new ListNode<Action, AntsActionConfigListItemNode> {
            ItemSpacing = 6.0f,
            OptionsList = [],
        };
        actionsListNode.AttachNode(ConfigurationContentNode);

        roleCategoryNode = new CategoryTextNode {
            TextId = 8576, // "Role Actions"
            SheetType = NodeData.SheetType.Addon,
        };
        roleCategoryNode.AttachNode(ConfigurationContentNode);

        rolesListNode = new ListNode<Action, AntsActionConfigListItemNode> {
            ItemSpacing = 6.0f,
            OptionsList = [],
        };
        rolesListNode.AttachNode(ConfigurationContentNode);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        var backgroundImageSize = Size * 3.0f / 4.0f;
        var minSize = MathF.Min(backgroundImageSize.X, backgroundImageSize.Y);
        var adjustedSize = new Vector2(minSize, minSize);

        backgroundImageNode.Size = adjustedSize;
        backgroundImageNode.Position = Size / 2.0f - backgroundImageNode.Size / 2.0f;

        actionsCategoryNode.Size = new Vector2(Width, 16.0f);
        actionsCategoryNode.Position = new Vector2(0.0f, 0.0f);

        actionsListNode.Size = new Vector2(Width, (Height - 32.0f - 6.0f) * 6.5f / 10.0f);
        actionsListNode.Position = new Vector2(0.0f, actionsCategoryNode.Bounds.Bottom + 12.0f);

        roleCategoryNode.Size = new Vector2(Width, 16.0f);
        roleCategoryNode.Position = new Vector2(0.0f, actionsListNode.Bounds.Bottom + 2.0f);

        rolesListNode.Size = new Vector2(Width, (Height - 32.0f - 6.0f) * 3.5f / 10.0f);
        rolesListNode.Position = new Vector2(0.0f, roleCategoryNode.Bounds.Bottom + 12.0f);
    }

    private readonly IconImageNode backgroundImageNode;

    private readonly CategoryTextNode actionsCategoryNode;
    private readonly ListNode<Action, AntsActionConfigListItemNode> actionsListNode;

    private readonly CategoryTextNode roleCategoryNode;
    private readonly ListNode<Action, AntsActionConfigListItemNode> rolesListNode;
}
