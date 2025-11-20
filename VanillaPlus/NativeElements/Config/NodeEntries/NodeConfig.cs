using System.Numerics;
using KamiToolKit.NodeBaseClasses;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Config.NodeEntries;

public class NodeConfig<T> : NodeConfigBase<T> where T : NodeBase, new() {

    private const float ElementStartOffset = 100.0f;

    protected override SimpleComponentNode? BuildOption(NodeConfigEnum configOption) => configOption switch {
        NodeConfigEnum.Position => BuildPositionEdit(),
        _ => null,
    };

    private LabelLayoutNode? BuildPositionEdit() {
        if (StyleObject is null) return null;

        var container = new LabelLayoutNode {
            Height = 28.0f,
            FillWidth = true,
        };

        var labelNode = new LabelTextNode {
            String = "Position",
            Size = new Vector2(ElementStartOffset, 28.0f),
        };
        container.AddNode(labelNode);

        var xPosition = new NumericInputNode {
            Height = 28.0f,
            Value = (int) StyleObject.X,
            Min = int.MinValue,
            OnValueUpdate = newValue => {
                StyleObject.X = newValue;
                SaveStyleObject();
            },
        };
        container.AddNode(xPosition);

        var yPosition = new NumericInputNode {
            Height = 28.0f,
            Min = int.MinValue,
            Value = (int) StyleObject.Y,
            OnValueUpdate = newValue => {
                StyleObject.Y = newValue;
                SaveStyleObject();
            },
        };
        container.AddNode(yPosition);

        return container;
    }
}
