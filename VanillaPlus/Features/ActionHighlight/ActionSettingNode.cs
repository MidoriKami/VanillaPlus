using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using Action = Lumina.Excel.Sheets.Action;

namespace VanillaPlus.Features.ActionHighlight;

public class ActionSettingNode : SimpleComponentNode {
    private readonly ActionHighlightConfig config;
    public Action Action { get; }

    private readonly CheckboxNode enabledCheckbox;
    private readonly IconImageNode iconNode;
    private readonly TextNode nameNode;
    private readonly NumericInputNode thresholdInput;

    public ActionSettingNode(ActionHighlightConfig config, Action action) {
        this.config = config;
        Action = action;

        var isEnabled = config.ActiveActions.ContainsKey(action.RowId);
        var threshold = isEnabled ? config.ActiveActions[action.RowId] : 3000;

        enabledCheckbox = new CheckboxNode {
            IsChecked = isEnabled,
            OnClick = OnCheckboxClicked,
        };
        enabledCheckbox.AttachNode(this);

        iconNode = new IconImageNode {
            IconId = action.Icon,
            Size = new Vector2(32.0f, 32.0f),
        };
        iconNode.AttachNode(this);

        nameNode = new TextNode {
            String = action.Name.ToString(),
            FontSize = 14,
            AlignmentType = AlignmentType.Left,
        };
        nameNode.AttachNode(this);

        thresholdInput = new NumericInputNode {
            Value = threshold,
            OnValueUpdate = OnThresholdChanged,
        };
        thresholdInput.AttachNode(this);
    }

    private void OnCheckboxClicked(bool isChecked) {
        if (isChecked) {
            config.ActiveActions[Action.RowId] = thresholdInput.Value;
        } else {
            config.ActiveActions.Remove(Action.RowId);
        }
        config.Save();
    }

    private void OnThresholdChanged(int newValue) {
        if (!config.ActiveActions.ContainsKey(Action.RowId)) return;

        config.ActiveActions[Action.RowId] = newValue;
        config.Save();
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        enabledCheckbox.Position = new Vector2(5.0f, (Height - 24.0f) / 2.0f);
        enabledCheckbox.Size = new Vector2(24.0f, 24.0f);

        iconNode.Position = new Vector2(35.0f, (Height - 32.0f) / 2.0f);

        thresholdInput.Size = new Vector2(120.0f, 30.0f);
        thresholdInput.Position = new Vector2(Width - 160.0f, (Height - 30.0f) / 2.0f);

        nameNode.Position = new Vector2(75.0f, (Height - 20.0f) / 2.0f);
        nameNode.Size = new Vector2(Width - 75.0f - 170.0f, 20.0f);
    }
}
