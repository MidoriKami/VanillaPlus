using System.Numerics;
using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Features.ConfigSearchBar;

public unsafe class TextEntry {
    public required string Text { get; init; }
    public required AtkTextNode* TextNode { get; init; }
    public required ConfigSearchBarConfig Config { get; init; }

    public void Deconstruct(out string text, out AtkTextNode* textNode) {
        text = Text;
        textNode = TextNode;
    }

    public bool IsMatch(Regex regex)
        => regex.IsMatch(Text);

    public void ApplyHighlight() {
        if (TextNode->ParentNode->GetNodeType() is NodeType.Component) {
            TextNode->ParentNode->MultiplyColor = Config.HighlightColor.AsVector3();
        }
        else {
            TextNode->AtkResNode.MultiplyColor = Config.HighlightColor.AsVector3();
        }
    }

    public void ClearHighlight() {
        if (TextNode->ParentNode->GetNodeType() is NodeType.Component) {
            TextNode->ParentNode->MultiplyColor = Vector3.One;
        }
        else {
            TextNode->AtkResNode.MultiplyColor = Vector3.One;
        }
    }
}
