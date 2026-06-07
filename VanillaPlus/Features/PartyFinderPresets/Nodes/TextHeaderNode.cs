using System.Numerics;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.PartyFinderPresets.Nodes;

public class TextHeaderNode : SimpleComponentNode {

    private readonly NineGridNode backgroundTexture;
    private readonly TextNode textNode;

    public TextHeaderNode() {
        backgroundTexture = new SimpleNineGridNode {
            Height = 20.0f,
            TexturePath = "ui/uld/PartyFinder.tex",
            TextureCoordinates = new Vector2(108.0f, 114.0f),
            TextureSize = new Vector2(20.0f, 20.0f),
            LeftOffset = 8,
            RightOffset = 8,
        };
        backgroundTexture.AttachNode(this);

        textNode = new TextNode {
            FontSize = 14,
            TextColor = ColorHelper.GetColor(22),
        };
        textNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        backgroundTexture.Size = Size;
        backgroundTexture.Position = Vector2.Zero;

        textNode.Size = new Vector2(Width - 20.0f, Height);
        textNode.Position = new Vector2(10.0f, 0.0f);
    }

    public ReadOnlySeString String {
        get => textNode.String;
        set => textNode.String = value;
    }
}
