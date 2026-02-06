using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow;

public sealed class DatasetNode : ResNode {
    public const float RowHeight = 26.0f;
    public readonly LabelTextNode CurrentLabelNode;
    public readonly IconImageNode IconNode;
    public readonly CosmicExplorationProgressBarNode MaxedProgressBarNode;
    public readonly LabelTextNode MaxLabelNode;
    public readonly CosmicExplorationProgressBarNode ProgressBarNode;
    public readonly LabelTextNode SlashLabelNode;

    public DatasetNode(float contentWidth) {
        Size = new Vector2(contentWidth, RowHeight);
        IconNode = new IconImageNode {
            IconId = 70812,
            Size = new Vector2(24.0f, 24.0f),
            Position = Vector2.Zero,
            ImageNodeFlags = ImageNodeFlags.AutoFit,
            WrapMode = WrapMode.Stretch,
        };
        IconNode.AttachNode(this);

        ProgressBarNode = new CosmicExplorationProgressBarNode {
            Position = new Vector2(28, 5),
            Size = new Vector2(0, 16),
            Progress = 1f,
            BarColor = CosmicExplorationProgressBarNode.BarType.Green,
        };
        ProgressBarNode.AttachNode(this);

        MaxedProgressBarNode = new CosmicExplorationProgressBarNode {
            Position = new Vector2(28, 5),
            Size = new Vector2(0, 16),
            Progress = 1f,
            BarColor = CosmicExplorationProgressBarNode.BarType.Red,
        };
        MaxedProgressBarNode.AttachNode(this);

        MaxLabelNode = new LabelTextNode {
            Position = new Vector2(contentWidth - 36, 0),
            Size = new Vector2(36, 24),
            TextColor = Vector4.One,
            TextOutlineColor = new Vector4(0f, 0.6f, 1f, 1f),
            FontType = FontType.TrumpGothic,
            FontSize = 20,
            AlignmentType = AlignmentType.Right,
            TextFlags = TextFlags.Edge | TextFlags.Glare,
        };
        MaxLabelNode.AttachNode(this);

        SlashLabelNode = new LabelTextNode {
            Position = new Vector2(MaxLabelNode.X - 10, 1),
            Size = new Vector2(10, 24),
            FontType = FontType.TrumpGothic,
            FontSize = 20,
            TextColor = Vector4.One,
            TextOutlineColor = new Vector4(0f, 0.6f, 1f, 1f),
            AlignmentType = AlignmentType.Right,
            TextFlags = TextFlags.Edge | TextFlags.Glare,
            String = "/",
        };
        SlashLabelNode.AddNodeFlags(NodeFlags.AnchorRight, NodeFlags.AnchorTop, NodeFlags.AnchorBottom);
        SlashLabelNode.AttachNode(this);

        CurrentLabelNode = new LabelTextNode {
            Position = new Vector2(SlashLabelNode.X - 36, 0),
            Size = new Vector2(36, 24),
            FontType = FontType.TrumpGothic,
            FontSize = 20,
            TextColor = Vector4.One,
            TextOutlineColor = new Vector4(0f, 0.6f, 1f, 1f),
            AlignmentType = AlignmentType.Right,
            TextFlags = TextFlags.Edge | TextFlags.Glare,
        };
        CurrentLabelNode.AddNodeFlags(NodeFlags.AnchorRight, NodeFlags.AnchorTop, NodeFlags.AnchorBottom);
        CurrentLabelNode.AttachNode(this);

        ProgressBarNode.Width = CurrentLabelNode.X - ProgressBarNode.X;
        MaxedProgressBarNode.Width = ProgressBarNode.Width;
    }

    public uint IconId {
        get => IconNode.IconId;
        set => IconNode.IconId = value;
    }

    public void UpdateData(CosmicExplorationProgressWindow.Progress p) {
        ProgressBarNode.Progress = p.Percentage;
        MaxedProgressBarNode.Progress = p.MaxPercentage;
        MaxedProgressBarNode.IsVisible = p.Complete;
        MaxedProgressBarNode.BackgroundImageNode.Width = MaxedProgressBarNode.ProgressNode.Width;
        IsVisible = p.Show;
        IconId = p.IconId;
        IconNode.TextTooltip = p.IconTooltip;

        CurrentLabelNode.String = $"{p.Current:N0}";
        MaxLabelNode.String = p.Complete ? $"{p.Max:N0}" : $"{p.Needed:N0}";

        if (p.Complete) {
            ProgressBarNode.BarColor = CosmicExplorationProgressBarNode.BarType.Blue;
            CurrentLabelNode.TextOutlineColor = new Vector4(0.6f, 0.6f, .2f, 1f);
        }
        else {
            ProgressBarNode.BarColor = CosmicExplorationProgressBarNode.BarType.Green;
            CurrentLabelNode.TextOutlineColor = new Vector4(0.4f, 0.4f, 0f, 1f);
            CurrentLabelNode.TextOutlineColor = new Vector4(0.0f, 0.6f, 1f, 1f);
        }

        if (p.Capped)
            CurrentLabelNode.TextOutlineColor = new Vector4(0.898f, 0f, 0.310f, 1f);
    }
}
