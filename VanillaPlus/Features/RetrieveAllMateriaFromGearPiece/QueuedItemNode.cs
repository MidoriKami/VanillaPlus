using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPiece;

public class QueuedItemNode : ListItemNode<QueuedItemNodeData> {
    public override float ItemHeight => 53.0f;

    private readonly IconImageNode iconNode;
    private readonly TextNode nameNode;
    private readonly TextNode statusNode;
    private readonly ProgressBarNode progressNode;
    private readonly TextNode progressTextNode;

    public QueuedItemNode() {
        ShowClickableCursor = false;
        DisableCollisionNode = true;

        iconNode = new IconImageNode {
            FitTexture = true,
        };
        iconNode.AttachNode(this);

        nameNode = new TextNode {
            AlignmentType = AlignmentType.BottomLeft,
            TextFlags = TextFlags.Ellipsis,
        };
        nameNode.AttachNode(this);

        statusNode = new TextNode {
            AlignmentType = AlignmentType.Left,
        };
        statusNode.AttachNode(this);

        progressNode = new ProgressBarNode {
            DisableCollisionNode = true,
        };
        progressNode.AttachNode(this);

        progressTextNode = new TextNode {
            AlignmentType = AlignmentType.Left,
        };
        progressTextNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        iconNode.Position = new Vector2(2.0f, 2.0f);
        iconNode.Size = new Vector2(48.0f, 48.0f);

        nameNode.Position = new Vector2(iconNode.Position.X + iconNode.Width + 4.0f, 0.0f);
        nameNode.Size = new Vector2(Width - nameNode.Position.X, Height / 2.0f);

        progressNode.Size = new Vector2(
            Width / 3,
            Height / 3
        );
        progressNode.Position = new Vector2(nameNode.Position.X, (Height / 2) + (progressNode.Height / 3));

        progressTextNode.Size = new Vector2(30f, Height / 2.0f);
        progressTextNode.Position = new Vector2(progressNode.Position.X + progressNode.Width + 4, Height / 2);

        statusNode.Size = new Vector2(50f - 4.0f, Height / 2);
        statusNode.Position = progressTextNode.Position + new Vector2(progressTextNode.Width + 4, 0f);
    }

    protected override void SetNodeData(QueuedItemNodeData itemData) {
        var excelSheet = Services.DataManager.Excel.GetSheet<Item>().GetRow(itemData.ItemId);

        iconNode.IconId = excelSheet.Icon;
        nameNode.String = excelSheet.Name;

        switch (itemData.Status) {
            case RetrievalAttemptStatus.NoAttemptMade:
                statusNode.String = "Pending";
                statusNode.TextColor = new Vector4(1f, 1f, 1f, 1f);
                break;
            case RetrievalAttemptStatus.RetrievedAll:
                statusNode.String = "Succeeded";
                statusNode.TextColor = new Vector4(0f, 1f, 0f, 1f);
                break;
            case RetrievalAttemptStatus.RetrievedSome:
            case RetrievalAttemptStatus.AttemptRunning:
            case RetrievalAttemptStatus.RetryNeeded:
                statusNode.String = "Retrieving...";
                statusNode.TextColor = new Vector4(0.4f, 0.4f, 1f, 1f);
                break;
            case RetrievalAttemptStatus.TimedOut:
                statusNode.String = "Failed";
                statusNode.TextColor = new Vector4(0.9f, 0.1f, 0.1f, 1f);
                break;
        }

        var progress = itemData.StartingMateriaCount - itemData.CurrentMateriaCount;

        progressTextNode.String = $"{progress} / {itemData.StartingMateriaCount}";
        progressNode.Progress = itemData.StartingMateriaCount == 0
                                    ? 0
                                    : (float)progress / itemData.StartingMateriaCount;
    }
}
