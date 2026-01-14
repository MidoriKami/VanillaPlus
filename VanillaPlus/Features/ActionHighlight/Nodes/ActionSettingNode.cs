using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using Action = Lumina.Excel.Sheets.Action;

namespace VanillaPlus.Features.ActionHighlight.Nodes;

public class ActionSettingNode : ListItemNode<ActionHighlightSetting> {
    public override float ItemHeight => 38.0f;

    private readonly CheckboxNode enabledCheckbox;
    private readonly IconImageNode iconNode;
    private readonly TextNode nameNode;
    private readonly NumericInputNode thresholdInput;

    public ActionSettingNode() {
        EnableSelection = false;
        EnableHighlight = false;
        DisableCollisionNode = true;

        enabledCheckbox = new CheckboxNode {
            OnClick = OnCheckboxClicked,
        };
        enabledCheckbox.AttachNode(this);

        iconNode = new IconImageNode {
            FitTexture = true,
            ShowClickableCursor = true,
            ActionTooltip = 1, // Sketch hack to make tooltips update correctly.
        };
        iconNode.AttachNode(this);

        nameNode = new TextNode {
            AlignmentType = AlignmentType.Left,
        };
        nameNode.AttachNode(this);

        thresholdInput = new NumericInputNode {
            OnValueUpdate = OnThresholdChanged,
        };
        thresholdInput.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        enabledCheckbox.Size = new Vector2(Height - 8.0f, Height - 8.0f);
        enabledCheckbox.Position = new Vector2(4.0f, 4.0f);

        iconNode.Size = new Vector2(Height, Height);
        iconNode.Position = new Vector2(enabledCheckbox.Bounds.Right + 8.0f, 0.0f);

        thresholdInput.Size = new Vector2(120.0f, Height - 8.0f);
        thresholdInput.Position = new Vector2(Width - thresholdInput.Width - 4.0f, 4.0f);

        nameNode.Size = new Vector2(Width - iconNode.Bounds.Right - thresholdInput.Width - 16.0f, Height);
        nameNode.Position = new Vector2(iconNode.Bounds.Right + 8.0f, 0.0f);
    }
    
    private bool isReloading;

    private void OnCheckboxClicked(bool isChecked) {
        if (isReloading) return;
        if (ItemData is null) return;
        
        ItemData.IsEnabled = isChecked;
        ItemData.ParentConfig?.Save();
    }

    private void OnThresholdChanged(int newValue) {
        if (isReloading) return;
        if (ItemData is null) return;
        
        ItemData.ThresholdMs = newValue;
        ItemData.ParentConfig?.Save();
    }

    protected override void SetNodeData(ActionHighlightSetting itemData) {
        isReloading = true;
        
        var actionData = Services.DataManager.GetExcelSheet<Action>().GetRow(itemData.ActionId);
        
        enabledCheckbox.IsChecked = itemData.IsEnabled;
        
        iconNode.HideTooltip();
        iconNode.IconId = actionData.Icon;
        iconNode.ActionTooltip = itemData.ActionId;

        nameNode.String = actionData.Name.ToString();
        
        thresholdInput.Value = itemData.ThresholdMs;
        
        isReloading = false;
    }
}
