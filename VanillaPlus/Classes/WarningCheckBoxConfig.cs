using System;
using System.Numerics;
using System.Threading.Tasks;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;

namespace VanillaPlus.Classes;

public class WarningCheckBoxConfig : BaseConfigEntry {
    public required bool InitialState { get; set; }
    public Action<bool>? ToggleAction { get; init; }
    public required uint WarningIconId { get; init; }
    public required string WarningTooltip { get; init; }

    public override NodeBase BuildNode() {
        var layoutNode = new HorizontalListNode {
            Height = 24.0f,
            ItemSpacing = 6.0f,
        };

        var checkboxNode = new CheckboxNode {
            OnClick = OnOptionChanged,
            Height = 24.0f,
            String = Label,
            IsChecked = InitialState,
        };

        var warningIconNode = new IconImageNode {
            IconId = WarningIconId,
            Size = new Vector2(20.0f, 20.0f),
            FitTexture = true,
            TextTooltip = WarningTooltip,
        };

        layoutNode.AddNode(checkboxNode);
        layoutNode.AddNode(warningIconNode);

        return layoutNode;
    }

    private void OnOptionChanged(bool newValue) {
        InitialState = newValue;
        ToggleAction?.Invoke(newValue);
        MemberInfo.SetValue(Config, newValue);
        Task.Run(Config.Save);
    }
}
