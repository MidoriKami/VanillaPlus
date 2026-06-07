using System;
using System.Numerics;
using System.Threading.Tasks;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class IntInputConfig : BaseConfigEntry {
    public required int InitialValue { get; set; }
    public required int Step { get; init; }
    public required Range Range { get; init; }

    public override NodeBase BuildNode() {
        var layoutNode = new HorizontalListNode {
            Height = 24.0f,
            ItemSpacing = 10.0f,
        };

        var numericInput = new NumericInputNode {
            Size = new Vector2(100.0f, 24.0f),
            Value = InitialValue,
            Step = Step,
            Min = Range.Start.Value,
            Max = Range.End.Value,
            OnValueUpdate = newValue => {
                InitialValue = newValue;
                MemberInfo.SetValue(Config, newValue);
                Task.Run(Config.Save);
            },
        };

        layoutNode.AddNode(numericInput);
        layoutNode.AddNode(GetLabelNode());

        return layoutNode;
    }
}
