using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.Basic_Nodes;

public class IconWithCountNode : ResNode {

    private readonly IconImageNode iconNode;
    private readonly TextNode countTextNode;

    public IconWithCountNode() {
        iconNode = new IconImageNode {
            IsVisible = true,
            FitTexture = true,
        };
        System.NativeController.AttachNode(iconNode, this);

        countTextNode = new TextNode {
            AlignmentType = AlignmentType.Right,
            TextFlags = TextFlags.Edge,
            FontSize = 12,
            IsVisible = true,
        };
        System.NativeController.AttachNode(countTextNode, this);
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
                if (value < 10000) {
                    countTextNode.String = value.ToString();
                }
                else {
                    countTextNode.String = $"{value / 1000,3}k";
                }
            }
        }
    }

    public bool ShowCountWhenOne { get; set; }
}
