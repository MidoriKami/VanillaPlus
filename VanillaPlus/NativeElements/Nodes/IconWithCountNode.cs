using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Nodes;

public class IconWithCountNode : SimpleComponentNode {

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
                countTextNode.IsVisible = true;
                countTextNode.String = value switch {
                    >= 1_000_000 => Strings.IconWithCount_MillionsFormat.Format(value / 1_000_000),
                    >= 10_000 => Strings.IconWithCount_ThousandsFormat.Format(value / 1_000),
                    _ => $"{value}",
                };
            }
            else {
                countTextNode.IsVisible = false;
            }
        }
    }

    public bool ShowCountWhenOne { get; set; }
}
