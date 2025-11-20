using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Nodes;

public class IconWithCountNode : ResNode {

    private readonly IconImageNode iconNode;
    private readonly TextNode countTextNode;

    public IconWithCountNode() {
        iconNode = new IconImageNode {
            FitTexture = true,
        };
        iconNode.AttachNode(this);

        countTextNode = new TextNode {
            AlignmentType = AlignmentType.Right,
            TextFlags = TextFlags.Edge,
            FontSize = 12,
        };
        countTextNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        iconNode.Size = Size - new Vector2(4.0f, 4.0f);
        iconNode.Position = new Vector2(2.0f, 2.0f);

        countTextNode.Size = new Vector2(Width, Height / 3.0f);
        countTextNode.Position = new Vector2(0.0f, Height * 2.0f / 3.0f);
    }

    public uint IconId {
        get => iconNode.IconId;
        set => iconNode.IconId = value;
    }

    public int Count {
        get => int.Parse(countTextNode.String);
        set {
            if (ShowCountWhenOne || value > 1) {
                countTextNode.String = value switch {
                    >= 1_000_000 => $"{value / 1_000_000}m",
                    >= 10_000 => $"{value / 1_000}k",
                    _ => $"{value}",
                };
            }
        }
    }

    public bool ShowCountWhenOne { get; set; }
}
