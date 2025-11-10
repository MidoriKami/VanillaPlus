using System;
using System.Numerics;
using Dalamud.Utility;
using KamiToolKit.Nodes;
using Lumina.Text;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.NativeElements.Nodes;

public class TextInputWithHintNode : SimpleComponentNode {
    private readonly TextInputNode textInputNode;
    private readonly ImageNode helpNode;

    public TextInputWithHintNode() {
        textInputNode = new TextInputNode {
            PlaceholderString = "Search . . .",
            IsVisible = true,
        };
        System.NativeController.AttachNode(textInputNode, this);

        helpNode = new SimpleImageNode {
            TexturePath = "ui/uld/CircleButtons.tex",
            TextureCoordinates = new Vector2(112.0f, 84.0f),
            TextureSize = new Vector2(28.0f, 28.0f),
            Tooltip = new SeStringBuilder()
                .Append("[VanillaPlus]: Supports Regex Search")
                .AppendNewLine()
                .Append("Start input with '$' to search by description")
                .ToReadOnlySeString()
                .ToDalamudString()
                .EncodeWithNullTerminator(),
            IsVisible = true,
        };
        System.NativeController.AttachNode(helpNode, this);
    }

    public required Action<ReadOnlySeString>? OnInputReceived {
        get => textInputNode.OnInputReceived;
        set => textInputNode.OnInputReceived = value;
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        helpNode.Size = new Vector2(Height, Height);
        helpNode.Position = new Vector2(Width - helpNode.Width - 5.0f, 0.0f);

        textInputNode.Size = new Vector2(Width - helpNode.Width - 5.0f, Height);
        textInputNode.Position = new Vector2(0.0f, 0.0f);
    }

    public ReadOnlySeString SearchString {
        get => textInputNode.SeString;
        set => textInputNode.SeString = value;
    }
}
