using System.Numerics;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow;

public class CosmicExplorationProgressBarNode : SimpleComponentNode {
    public enum BarType {
        Green = 0,
        Yellow = 2,
        Blue = 3,
        Teal = 4,
        Red = 5,
        White = 6,
    }

    private const string SynthesisTexPath = "ui/uld/Synthesis.tex";

    public readonly SimpleNineGridNode BackgroundImageNode;
    public readonly SimpleNineGridNode BorderImageNode;
    public readonly SimpleNineGridNode ProgressNode;

    public CosmicExplorationProgressBarNode() {
        BackgroundImageNode = new SimpleNineGridNode {
            Size = new Vector2(170.0f, 16.0f),
            TextureCoordinates = new Vector2(0.0f, 32.0f),
            TextureSize = new Vector2(64.0f, 16.0f),
            TexturePath = SynthesisTexPath,
            Offsets = new Vector4(2, 3, 8, 8),
            PartsRenderType = 0,
        };
        BackgroundImageNode.AttachNode(this);

        ProgressNode = new SimpleNineGridNode {
            Position = new Vector2(0, 2),
            Size = new Vector2(0, 12.0f),
            TextureCoordinates = new Vector2(0.0f, 60.0f),
            TextureSize = new Vector2(64.0f, 12.0f),
            TexturePath = SynthesisTexPath,
            Offsets = new Vector4(2, 3, 8, 8),
            PartsRenderType = 0x10,
        };
        ProgressNode.AttachNode(this);

        BorderImageNode = new SimpleNineGridNode {
            Position = new Vector2(0, 2),
            Size = new Vector2(0, 12.0f),
            TextureCoordinates = new Vector2(0.0f, 108.0f),
            TextureSize = new Vector2(64.0f, 12.0f),
            TexturePath = SynthesisTexPath,
            Offsets = new Vector4(2, 3, 8, 8),
            PartsRenderType = 0,
        };
        BorderImageNode.AttachNode(this);
    }

    public float Progress {
        get => ProgressNode.Width / Width;
        set => BorderImageNode.Width = ProgressNode.Width = value * Width;
    }

    public BarType BarColor {
        get;
        set {
            if (field == value) return;
            field = value;

            ProgressNode.V = value switch {
                BarType.Green => 60f,
                BarType.Yellow => 72f,
                BarType.Blue => 48f,
                BarType.Teal => 84f,
                BarType.Red => 96f,
                BarType.White => 140f,
                _ => ProgressNode.V,
            };
        }
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        var p = ProgressNode.Width / BackgroundImageNode.Width;
        BackgroundImageNode.Size = Size;
        BorderImageNode.Height = ProgressNode.Height = Height - 4;
        Progress = p;
    }
}
