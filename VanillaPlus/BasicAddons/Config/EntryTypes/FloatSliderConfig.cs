﻿using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.System;

namespace VanillaPlus.BasicAddons.Config.EntryTypes;

public class FloatSliderConfig : BaseConfigEntry {
    public required float MinValue { get; init; }
    public required float MaxValue { get; init; }
    public required float InitialValue { get; init; }
    public required int DecimalPlaces { get; init; }
    public required float StepSpeed { get; init; }
    
    public override NodeBase BuildNode() {
        var layoutNode = new HorizontalListNode {
            Height = 24.0f,
            IsVisible = true,
            ItemSpacing = 20.0f,
        };

        var sliderNode = new SliderNode {
            Size = new Vector2(175.0f, 24.0f),
            IsVisible = true,
            DecimalPlaces = DecimalPlaces,
            Range = (int)(MinValue * Math.Pow(10, DecimalPlaces))..(int)(MaxValue * Math.Pow(10, DecimalPlaces)),
            OnValueChanged = newValue => OnOptionChanged(newValue / MathF.Pow(10, DecimalPlaces)),
            Value = (int)(InitialValue * Math.Pow(10, DecimalPlaces)),
            Step = (int)(StepSpeed * MathF.Pow(10, DecimalPlaces)),
        };

        var labelNode = new SimpleLabelNode {
            IsVisible = true,
            Size = new Vector2(0.0f, 24.0f),
            AlignmentType = AlignmentType.TopLeft,
            String = Label,
        };

        layoutNode.AddNode(sliderNode);
        layoutNode.AddNode(labelNode);

        return layoutNode;
    }

    private void OnOptionChanged(float newValue) {
        MemberInfo.SetValue(Config, newValue);
        Config.Save();
    }
}
