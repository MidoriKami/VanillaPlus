using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.QuestListWindow;

public unsafe class QuestEntryNode : SimpleComponentNode {

    private readonly NineGridNode hoveredBackgroundNode;
    private readonly IconImageNode questIconNode;
    private readonly TextNode questNameTextNode;
    private readonly TextNode questLevelTextNode;
    private readonly TextNode issuerNameTextNode;
    private readonly TextNode distanceTextNode;

    public QuestEntryNode() {
        hoveredBackgroundNode = new SimpleNineGridNode {
            TexturePath = "ui/uld/ListItemA.tex",
            TextureCoordinates = new Vector2(0.0f, 22.0f),
            TextureSize = new Vector2(64.0f, 22.0f),
            TopOffset = 6,
            BottomOffset = 6,
            LeftOffset = 16,
            RightOffset = 1,
            IsVisible = false,
        };
        hoveredBackgroundNode.AttachNode(this);
        
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

        CollisionNode.DrawFlags |= DrawFlags.ClickableCursor;
        CollisionNode.AddEvent(AtkEventType.MouseOver, () => IsHovered = true);
        CollisionNode.AddEvent(AtkEventType.MouseOut, () => IsHovered = false);
        CollisionNode.AddEvent(AtkEventType.MouseClick, () => {
            if (QuestInfo is null) return;

            var agentMap = AgentMap.Instance();
            if (agentMap is not null) {
                agentMap->FlagMarkerCount = 0;
                agentMap->SetFlagMapMarker(agentMap->CurrentTerritoryId, agentMap->CurrentMapId, QuestInfo.Position, QuestInfo.IconId);
                agentMap->OpenMap(agentMap->CurrentMapId, agentMap->CurrentTerritoryId, QuestInfo.Name.ToString(), MapType.QuestLog);
                agentMap->OpenMap(agentMap->CurrentMapId, agentMap->CurrentTerritoryId, QuestInfo.Name.ToString());
            }
        });
    }

    public required QuestInfo QuestInfo {
        get;
        set {
            field = value;

            if (value.Level > 0) {
                questLevelTextNode.String = $"Lv. {value.Level}";
            }
            else {
                questNameTextNode.Width = Width - questIconNode.Width - 4.0f;
            }
            
            questIconNode.IconId = value.IconId;
            questNameTextNode.SeString = value.Name;
            issuerNameTextNode.SeString = value.IssuerName;
        }
    }

    public bool IsHovered {
        get => hoveredBackgroundNode.IsVisible;
        set => hoveredBackgroundNode.IsVisible = value;
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        hoveredBackgroundNode.Size = Size;
        
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

    public void Update()
        => distanceTextNode.String = $"{QuestInfo.Distance:F1} y";
}
