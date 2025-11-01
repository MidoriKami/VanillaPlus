using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.System;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class IntSliderConfig : BaseConfigEntry {
    public required Range Range { get; init; }
    public required int InitialValue { get; init; }

    public override NodeBase BuildNode() {
        var layoutNode = new HorizontalListNode {
            Height = 24.0f,
            IsVisible = true,
            ItemSpacing = 20.0f,
        };

        var sliderNode = new SliderNode {
            Size = new Vector2(175.0f, 24.0f),
            IsVisible = true,
            Range = Range,
            OnValueChanged = OnOptionChanged,
            Value = InitialValue,
        };

        var labelNode = new CategoryTextNode {
            IsVisible = true,
            Size = new Vector2(0.0f, 24.0f),
            String = Label,
            AlignmentType = AlignmentType.TopLeft,
        };
            
        layoutNode.AddNode(sliderNode);
        layoutNode.AddNode(labelNode);

        return layoutNode;
    }

    private void OnOptionChanged(int newValue) {
        MemberInfo.SetValue(Config, newValue);
        Config.Save();
    }
}
