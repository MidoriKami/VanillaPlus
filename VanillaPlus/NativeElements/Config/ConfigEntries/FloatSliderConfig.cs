using System.Numerics;
using System.Threading.Tasks;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class FloatSliderConfig : BaseConfigEntry {
    public required float MinValue { get; init; }
    public required float MaxValue { get; init; }
    public required float InitialValue { get; set; }
    public required float StepSpeed { get; init; }

    public override NodeBase BuildNode() {
        var layoutNode = new HorizontalListNode {
            Height = 24.0f,
            ItemSpacing = 40.0f,
        };

        var sliderNode = new FloatSliderNode {
            Size = new Vector2(175.0f, 24.0f),
            Position = new Vector2(0.0f, 4.0f),
            Min = MinValue,
            Max = MaxValue,
            OnValueChanged = OnOptionChanged,
            Value = InitialValue,
            Step = StepSpeed,
        };

        layoutNode.AddNode(sliderNode);
        layoutNode.AddNode(GetLabelNode());

        return layoutNode;
    }

    private void OnOptionChanged(float newValue) {
        InitialValue = newValue;
        MemberInfo.SetValue(Config, newValue);
        Task.Run(Config.Save);
    }
}
