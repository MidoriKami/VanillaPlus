using System.Numerics;
using KamiToolKit.Nodes;
using VanillaPlus.Features.CosmicExplorationProgressWindow.Enums;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow.Nodes;

public class WksProgressBarNode : SimpleComponentNode {
    private const string SynthesisTexPath = "ui/uld/Synthesis.tex";

    private readonly SimpleNineGridNode backgroundImageNode;
    private readonly SimpleNineGridNode borderImageNode;
    private readonly SimpleNineGridNode progressNode;

    public WksProgressBarNode() {
        backgroundImageNode = new SimpleNineGridNode {
            Size = new Vector2(170.0f, 16.0f),
            TextureCoordinates = new Vector2(0.0f, 32.0f),
            TextureSize = new Vector2(64.0f, 16.0f),
            TexturePath = SynthesisTexPath,
            Offsets = new Vector4(2.0f, 3.0f, 8.0f, 8.0f),
        };
        backgroundImageNode.AttachNode(this);

        progressNode = new SimpleNineGridNode {
            Position = new Vector2(0.0f, 2.0f),
            Size = new Vector2(0.0f, 12.0f),
            TextureCoordinates = new Vector2(0.0f, 60.0f),
            TextureSize = new Vector2(64.0f, 12.0f),
            TexturePath = SynthesisTexPath,
            Offsets = new Vector4(2.0f, 3.0f, 8.0f, 8.0f),
            PartsRenderType = 0x10,
        };
        progressNode.AttachNode(this);

        borderImageNode = new SimpleNineGridNode {
            Position = new Vector2(0.0f, 2.0f),
            Size = new Vector2(0.0f, 12.0f),
            TextureCoordinates = new Vector2(0.0f, 108.0f),
            TextureSize = new Vector2(64.0f, 12.0f),
            TexturePath = SynthesisTexPath,
            Offsets = new Vector4(2.0f, 3.0f, 8.0f, 8.0f),
        };
        borderImageNode.AttachNode(this);
    }

    public bool IsTopLayerNode { get; set; }
    
    public float Progress {
        get => progressNode.Width / Width;
        set {
            borderImageNode.Width = progressNode.Width = value * Width;

            if (IsTopLayerNode) {
                backgroundImageNode.Width = borderImageNode.Width;
            }
        }
    }

    public BarType BarColor {
        set {
            if (field == value) return;
            field = value;

            progressNode.V = value switch {
                BarType.Green => 60.0f,
                BarType.Yellow => 72.0f,
                BarType.Blue => 48.0f,
                BarType.Teal => 84.0f,
                BarType.Red => 96.0f,
                BarType.White => 140.0f,
                _ => progressNode.V,
            };
        }
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        var progress = progressNode.Width / backgroundImageNode.Width;
        backgroundImageNode.Size = Size;
        progressNode.Height = Height - 4.0f;
        borderImageNode.Height = Height - 4.0f;
        Progress = progress;
    }
}
