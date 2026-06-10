using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.Features.ActionHighlight.Config;

namespace VanillaPlus.Features.ActionHighlight.Nodes;

/// <summary>
/// Implementation of <see cref="ListItemNode{T}"/> for use in <see cref="ActionHighlight"/>
/// </summary>
public class AntsActionConfigListItemNode : ListItemNode<Action>, IListItemNode {

    /// <inheritdoc/>
    public static float ItemHeight => 48.0f;

    /// <inheritdoc/>
    protected override void SetNodeData(Action itemData) {
        if (ActionHighlight.Config is not { } config) return;
        if (AntsClassJobConfigurationNode.ClassJobConfig is not { } classJobConfig) return;

        var actionSetting = classJobConfig.ActionSettings.FirstOrDefault(actionSetting => actionSetting.ActionId == itemData.RowId);

        checkboxNode.OnClick = null;
        checkboxNode.IsChecked = actionSetting?.IsEnabled ?? false;
        checkboxNode.OnClick = newValue => {
            if (actionSetting is not null) {
                actionSetting.IsEnabled = newValue;
            }
            else {
                actionSetting = new AntsActionSetting {
                    IsEnabled = true,
                    ActionId = itemData.RowId,
                    ThresholdMs = config.PreAntTimeMs,
                };
                classJobConfig.ActionSettings.Add(actionSetting);
            }
            Task.Run(config.Save);
        };

        actionIconNode.IconId = itemData.Icon;
        actionIconNode.ActionTooltip = itemData.RowId;

        actionNameNode.String = itemData.Name;

        delayTimeNode.OnValueUpdate = null;
        delayTimeNode.Value = actionSetting?.ThresholdMs ?? config.PreAntTimeMs;
        delayTimeNode.OnValueUpdate = newValue => {
            if (actionSetting is not null) {
                actionSetting.ThresholdMs = newValue;
            }
            else {
                actionSetting = new AntsActionSetting {
                    IsEnabled = false,
                    ActionId = itemData.RowId,
                    ThresholdMs = config.PreAntTimeMs,
                };
                classJobConfig.ActionSettings.Add(actionSetting);
            }
            Task.Run(config.Save);
        };

        OnClick = _ => {
            if (actionSetting is not null) {
                actionSetting.IsEnabled = !checkboxNode.IsChecked;
            }
            else {
                actionSetting = new AntsActionSetting {
                    IsEnabled = true,
                    ActionId = itemData.RowId,
                    ThresholdMs = config.PreAntTimeMs,
                };
                classJobConfig.ActionSettings.Add(actionSetting);
            }

            checkboxNode.IsChecked = actionSetting.IsEnabled;
            Task.Run(config.Save);

            IsSelected = false;
            IsHovered = true;
        };
    }

    public AntsActionConfigListItemNode() {
        checkboxNode = new CheckboxNode();
        checkboxNode.AttachNode(this);

        actionIconNode = new IconImageNode {
            FitTexture = true,
            ActionTooltip = 1,
            DrawFlags = DrawFlags.ClickableCursor,
        };
        actionIconNode.AttachNode(this);

        actionNameNode = new TextNode {
            TextFlags = TextFlags.Ellipsis,
        };
        actionNameNode.AttachNode(this);

        delayTimeNode = new NumericInputNode();
        delayTimeNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        checkboxNode.Size = new Vector2(Height / 2.0f, Height / 2.0f);
        checkboxNode.Position = checkboxNode.Size / 2.0f;

        actionIconNode.Size = new Vector2(Height, Height);
        actionIconNode.Position = new Vector2(Height, 0.0f);

        delayTimeNode.Size = new Vector2(150.0f, Height / 2.0f);
        delayTimeNode.Position = new Vector2(Width - delayTimeNode.Width - 4.0f, Height / 4.0f);

        actionNameNode.Size = new Vector2(delayTimeNode.Bounds.Left - actionIconNode.Bounds.Right - 4.0f, Height);
        actionNameNode.Position = new Vector2(actionIconNode.Bounds.Right + 2.0f, 0.0f);
    }

    private readonly CheckboxNode checkboxNode;
    private readonly IconImageNode actionIconNode;
    private readonly TextNode actionNameNode;
    private readonly NumericInputNode delayTimeNode;
}
