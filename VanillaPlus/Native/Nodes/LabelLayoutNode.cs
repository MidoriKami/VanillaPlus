using System.Linq;
using System.Numerics;
using KamiToolKit.BaseTypes.ComponentNode;
using KamiToolKit.Nodes;

namespace VanillaPlus.Native.Nodes;

public class LabelLayoutNode : LayoutListNode {

    public bool FillWidth { get; set; }

    public int NavUp { get; set; }
    public int NavDown { get; set; }

    protected override void OnRecalculateLayout() {
        if (Nodes.Count is 0) return;

        var labelNode = Nodes[0];

        var labelNodeWidth = labelNode.Width;
        labelNode.Position = new Vector2(0.0f, 0.0f);

        var position = labelNodeWidth + FirstItemSpacing;
        foreach (var node in Nodes.Skip(1)) {
            node.X = position;

            if (FillWidth) {
                node.Width = (Width - labelNodeWidth - FirstItemSpacing) / (Nodes.Count - 1);
            }

            position += node.Width + ItemSpacing;
        }
    }

    protected override void OnRecalculateNavigation() {
        var componentNodes = NodeList.OfType<ComponentNode>().ToList();
        if (componentNodes.Count is 0) return;

        foreach (var (index, node) in componentNodes.Index()) {
            node.NavIndex = index + NavIndex;
            node.NavUp = NavUp;
            node.NavDown = NavDown;

            // First Element
            if (index is 0) {
                node.NavLeft = componentNodes.Count - 1 + NavIndex;
            }
            else {
                node.NavLeft = index - 1 + NavIndex;
            }

            // Last Element
            if (index == componentNodes.Count - 1) {
                node.NavRight = NavIndex;
            }
            else {
                node.NavRight = index + 1 + NavIndex;
            }
        }
    }
}
