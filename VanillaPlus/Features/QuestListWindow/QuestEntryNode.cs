using System.Numerics;
using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Client.UI;
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
            NodeId = 2,
            TexturePath = "ui/uld/ListItemA.tex",
            TextureCoordinates = new Vector2(0.0f, 22.0f),
            TextureSize = new Vector2(64.0f, 22.0f),
            TopOffset = 6,
            BottomOffset = 6,
            LeftOffset = 16,
            RightOffset = 1,
            IsVisible = false,
        };
        System.NativeController.AttachNode(hoveredBackgroundNode, this);
        
        questIconNode = new IconImageNode {
            NodeId = 3,
            IsVisible = true,
        };
        System.NativeController.AttachNode(questIconNode, this);

        questNameTextNode = new TextNode {
            NodeId = 4,
            AlignmentType = AlignmentType.BottomLeft,
            TextFlags = TextFlags.Ellipsis,
            FontSize = 13,
            IsVisible = true,
        };
        System.NativeController.AttachNode(questNameTextNode, this);

        issuerNameTextNode = new TextNode {
            NodeId = 5,
            AlignmentType = AlignmentType.TopLeft,
            TextColor = ColorHelper.GetColor(2),
            TextFlags = TextFlags.Ellipsis,
            FontSize = 12,
            IsVisible = true,
        };
        System.NativeController.AttachNode(issuerNameTextNode, this);
        
        questLevelTextNode = new TextNode {
            NodeId = 6,
            AlignmentType = AlignmentType.BottomLeft,
            FontSize = 13,
            IsVisible = true,
        };
        System.NativeController.AttachNode(questLevelTextNode, this);

        distanceTextNode = new TextNode {
            NodeId = 7,
            AlignmentType = AlignmentType.TopRight,
            TextColor = ColorHelper.GetColor(2),
            IsVisible = true,
        };
        System.NativeController.AttachNode(distanceTextNode, this);
        
        CollisionNode.AddEvent(AddonEventType.MouseOver, _ => {
            IsHovered = true;
        });
        
        CollisionNode.AddEvent(AddonEventType.MouseClick, _ => {
            if (QuestInfo is null) return;

            var agentMap = AgentMap.Instance();
            agentMap->OpenMap(agentMap->CurrentMapId, agentMap->CurrentTerritoryId, QuestInfo.Name.ToString());
            agentMap->FlagMarkerCount = 0;
            agentMap->SetFlagMapMarker(agentMap->CurrentTerritoryId, agentMap->CurrentMapId, QuestInfo.Position, QuestInfo.IconId);
            RaptureAtkModule.Instance()->FocusAddon(agentMap->AddonId);
        });
        
        CollisionNode.AddEvent(AddonEventType.MouseOut, _ => {
            IsHovered = false;
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
            questNameTextNode.ReadOnlySeString = value.Name;
            issuerNameTextNode.ReadOnlySeString = value.IssuerName;
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
