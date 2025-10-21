using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.System;

namespace VanillaPlus.NativeElements.Config.ConfigEntries;

public class SelectIconConfig : BaseConfigEntry {
    public required uint InitialIcon { get; init; }

    public override NodeBase BuildNode() {
        var layoutNode = new HorizontalListNode {
            Height = 50.0f,
            IsVisible = true,
            ItemSpacing = 20.0f,
        };

        var iconImageNode = new IconImageNode {
            Size = new Vector2(50.0f, 50.0f),
            IconId = InitialIcon,
            IsVisible = true,
            FitTexture = true,
        };
        layoutNode.AddNode(iconImageNode);

        var verticalLayout = new VerticalListNode {
            Size = new Vector2(100.0f, 50.0f),
            IsVisible = true,
            ItemSpacing = 2.0f,
        };

        var labelNode = GetLabelNode();
        labelNode.AlignmentType = AlignmentType.BottomLeft;
        verticalLayout.AddNode(labelNode);

        var inputIntNode = new NumericInputNode {
            Size = new Vector2(125.0f, 24.0f),
            Height = 24.0f,
            IsVisible = true,
            Value = (int) InitialIcon,
            OnValueUpdate = newValue => {
                iconImageNode.IconId = (uint) newValue;
                MemberInfo.SetValue(Config, (uint) newValue);
                Config.Save();
            },
        };
        
        verticalLayout.AddNode(inputIntNode);
        layoutNode.AddNode(verticalLayout);
        
        return layoutNode;
    }
}
