using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using VanillaPlus.Features.CosmicExplorationProgressWindow.Classes;

namespace VanillaPlus.Features.CosmicExplorationProgressWindow.Nodes;

public sealed class WksProgressListItemNode : ListItemNode<Progress> {
    private readonly TextNode currentLabelNode;
    private readonly IconImageNode iconNode;
    private readonly TextNode maxLabelNode;
    private readonly WksCompositeProgressBarNode progressBarNode;

    private readonly HorizontalListNode layoutNode; 
    
    public override float ItemHeight => 26.0f;

    public WksProgressListItemNode() {
        DisableInteractions();
        
        layoutNode = new HorizontalListNode {
            ItemSpacing = 5.0f,
            InitialNodes = [
                iconNode = new IconImageNode {
                    Size = new Vector2(24.0f, 24.0f),
                    IconId = 70812,
                    ImageNodeFlags = ImageNodeFlags.AutoFit,
                    WrapMode = WrapMode.Stretch,
                    TextTooltip = ":)",
                },
                progressBarNode = new WksCompositeProgressBarNode {
                    Size = new Vector2(142.0f, 16.0f),
                    Y = 5.0f,
                    Progress = 1.0f,
                    MaxProgress = 1.0f,
                },
                currentLabelNode = new TextNode {
                    Size = new Vector2(26.0f, 26.0f),
                    TextColor = ColorHelper.GetColor(1),
                    TextOutlineColor = ColorHelper.GetColor(707),
                    FontType = FontType.TrumpGothic,
                    FontSize = 20,
                    AlignmentType = AlignmentType.Right,
                    TextFlags = TextFlags.Edge | TextFlags.Glare,
                    String = "UNK",
                },
                new TextNode {
                    Size = new Vector2(8.0f, 26.0f),
                    FontType = FontType.TrumpGothic,
                    FontSize = 20,
                    TextColor = ColorHelper.GetColor(1),
                    TextOutlineColor = ColorHelper.GetColor(707),
                    AlignmentType = AlignmentType.Right,
                    TextFlags = TextFlags.Edge | TextFlags.Glare,
                    String = "/",
                },
                maxLabelNode = new TextNode {
                    Size = new Vector2(26.0f, 26.0f),
                    FontType = FontType.TrumpGothic,
                    FontSize = 20,
                    TextColor = ColorHelper.GetColor(1),
                    TextOutlineColor = ColorHelper.GetColor(707),
                    AlignmentType = AlignmentType.Right,
                    TextFlags = TextFlags.Edge | TextFlags.Glare,
                    String = "UNK",
                },
            ],
        };
        layoutNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        layoutNode.Size = Size;
        layoutNode.RecalculateLayout();
    }

    public uint IconId {
        get => iconNode.IconId;
        set => iconNode.IconId = value;
    }

    public override void Update() {
        if (ItemData is null) return;
        
        progressBarNode.Progress = ItemData.Percentage;
        progressBarNode.MaxProgress = ItemData.MaxPercentage;
        progressBarNode.IsComplete = ItemData.Complete;

        currentLabelNode.String = $"{ItemData.Current:N0}";
        maxLabelNode.String = ItemData.Complete ? $"{ItemData.Max:N0}" : $"{ItemData.Needed:N0}";

        if (ItemData.Complete) {
            currentLabelNode.TextOutlineColor = new Vector4(0.6f, 0.6f, .2f, 1f);
        }
        else {
            currentLabelNode.TextOutlineColor = new Vector4(0.0f, 0.6f, 1f, 1f);
        }

        if (ItemData.Capped) {
            currentLabelNode.TextOutlineColor = new Vector4(0.898f, 0f, 0.310f, 1f);
        }
    }

    protected override void SetNodeData(Progress progressData) {
        IconId = progressData.IconId;
        iconNode.TextTooltip = progressData.IconTooltip;
        
        Update();
    }
}
