using System.Numerics;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using VanillaPlus.Features.AdditionalHotbarSlots.Config;

namespace VanillaPlus.Features.AdditionalHotbarSlots.Nodes;

public class HotbarListItemNode : ListItemNode<HotbarConfig>, IListItemNode {

    public static float ItemHeight => 32.0f;

    public override void Update() {
        base.Update();

        textNode.String = ItemData?.Name;
    }

    protected override void SetNodeData(HotbarConfig itemData) {
        textNode.String = itemData.Name;
    }

    public HotbarListItemNode() {
        textNode = new TextNode();
        textNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        textNode.Size = new Vector2(Width - 10.0f, Height);
        textNode.Position = new Vector2(10.0f, 0.0f);
    }

    private readonly TextNode textNode;
}
