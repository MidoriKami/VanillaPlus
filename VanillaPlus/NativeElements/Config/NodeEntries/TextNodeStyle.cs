using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Config.NodeEntries;

public class TextNodeStyle : NodeStyle<TextNode> {
    public Vector4 TextColor { get; set; }
    public Vector4 TextOutlineColor { get; set; }
    public uint FontSize { get; set; }
    public FontType FontType { get; set; }
    public AlignmentType AlignmentType { get; set; }

    public override void ApplyStyle(TextNode? node) {
        base.ApplyStyle(node);

        node?.TextColor = TextColor;
        node?.TextOutlineColor = TextOutlineColor;
        node?.FontSize = FontSize;
        node?.FontType = FontType;
        node?.AlignmentType = AlignmentType;
    }
}
