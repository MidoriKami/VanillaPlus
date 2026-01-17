using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.QuestListWindow;

public class QuestListItemNode : ListItemNode<MarkerInfo> {
    public override float ItemHeight => 48.0f;
    
    private readonly IconImageNode questIconNode;
    private readonly TextNode questNameTextNode;
    private readonly TextNode questLevelTextNode;
    private readonly TextNode issuerNameTextNode;
    private readonly TextNode distanceTextNode;

    public QuestListItemNode() {
        questIconNode = new IconImageNode {
            FitTexture = true,
        };
        questIconNode.AttachNode(this);

        questNameTextNode = new TextNode {
            AlignmentType = AlignmentType.BottomLeft,
            TextFlags = TextFlags.Ellipsis,
            FontSize = 13,
        };
        questNameTextNode.AttachNode(this);

        issuerNameTextNode = new TextNode {
            AlignmentType = AlignmentType.TopLeft,
            TextColor = ColorHelper.GetColor(2),
            TextFlags = TextFlags.Ellipsis,
            FontSize = 12,
        };
        issuerNameTextNode.AttachNode(this);
        
        questLevelTextNode = new TextNode {
            AlignmentType = AlignmentType.BottomLeft,
            FontSize = 13,
        };
        questLevelTextNode.AttachNode(this);

        distanceTextNode = new TextNode {
            AlignmentType = AlignmentType.TopRight,
            TextColor = ColorHelper.GetColor(2),
        };
        distanceTextNode.AttachNode(this);
        
        CollisionNode.AddEvent(AtkEventType.MouseClick, () => ItemData.FocusMarker());
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        
        questIconNode.Size = new Vector2(Height, Height);
        questIconNode.Position = Vector2.Zero;

        questLevelTextNode.Size = new Vector2(45.0f, Height / 2.0f);
        questLevelTextNode.Position = new Vector2(Width - questLevelTextNode.Width - 4.0f, 0.0f);
        
        questNameTextNode.Size = new Vector2(Width - questIconNode.Width - questLevelTextNode.Width - 8.0f, Height / 2.0f);
        questNameTextNode.Position = new Vector2(questIconNode.Width + 4.0f, 0.0f);

        issuerNameTextNode.Size = new Vector2(Width - questIconNode.Width - questLevelTextNode.Width - 16.0f, Height / 2.0f);
        issuerNameTextNode.Position = new Vector2(questIconNode.Width + 12.0f, Height / 2.0f);

        distanceTextNode.Size = questLevelTextNode.Size;
        distanceTextNode.Position = new Vector2(Width - questLevelTextNode.Width - 4.0f, Height / 2.0f);
    }

    protected override void SetNodeData(MarkerInfo itemData) {
        if (itemData.ClassJobLevel > 0) {
            questLevelTextNode.String = $"Lv. {itemData.ClassJobLevel}";
        }
        else {
            questNameTextNode.Width = Width - questIconNode.Width - 4.0f;
        }
        
        questIconNode.IconId = itemData.IconId;
        questNameTextNode.String = itemData.Name;
        issuerNameTextNode.String = itemData.IssuerName;
    }

    public override void Update()
        => distanceTextNode.String = $"{ItemData.Distance:F1} y";
}
