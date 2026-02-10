using KamiToolKit.Nodes;
using VanillaPlus.Features.CosmicExplorationProgressWindow.Enums;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow.Nodes;

public class WksCompositeProgressBarNode : SimpleComponentNode {

    private readonly WksProgressBarNode progressBarNode;
    private readonly WksProgressBarNode maxedProgressBarNode;

    public WksCompositeProgressBarNode() {
        progressBarNode = new WksProgressBarNode {
            Width = 16.0f,
            Progress = 1.0f,
            BarColor = BarType.Green,
        };
        progressBarNode.AttachNode(this);

        maxedProgressBarNode = new WksProgressBarNode {
            Width = 16.0f,
            Progress = 1.0f,
            BarColor = BarType.Red,
            IsTopLayerNode = true,
        };
        maxedProgressBarNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        progressBarNode.Size = Size;
        maxedProgressBarNode.Size = Size;
    }

    public float Progress {
        get => progressBarNode.Progress;
        set => progressBarNode.Progress = value;
    }

    public float MaxProgress {
        get => maxedProgressBarNode.Progress;
        set => maxedProgressBarNode.Progress = value;
    }
    
    public bool IsComplete {
        get;
        set {
            field = value;
            maxedProgressBarNode.IsVisible = value;
            progressBarNode.BarColor = value ? BarType.Blue : BarType.Green;
        }
    }
}
