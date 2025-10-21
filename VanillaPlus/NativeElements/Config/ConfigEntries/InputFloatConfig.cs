using System;
using System.Numerics;
using KamiToolKit.Nodes;
using KamiToolKit.System;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class FloatInputConfig : BaseConfigEntry {
    public required float InitialValue { get; init; }
    public required int Step { get; init; }
    public required Range Range { get; init; }

    public override NodeBase BuildNode() {
        var layoutNode = new HorizontalListNode {
            Height = 24.0f,
            IsVisible = true,
            ItemSpacing = 10.0f,
        };

        var numericInput = new NumericInputNode {
            Size = new Vector2(100.0f, 24.0f),
            Value = (int) InitialValue,
            IsVisible = true,
            Step = Step,
            Min = Range.Start.Value,
            Max = Range.End.Value,
            OnValueUpdate = newValue => {
                MemberInfo.SetValue(Config, newValue);
                Config.Save();
            },
        };

        layoutNode.AddNode(numericInput);
        layoutNode.AddNode(GetLabelNode());
        
        return layoutNode;
    }
}
