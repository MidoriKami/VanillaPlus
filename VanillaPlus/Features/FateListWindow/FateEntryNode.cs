using System;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes.TimelineBuilding;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.FateListWindow;

public unsafe class FateEntryNode : SimpleComponentNode {

    private readonly NineGridNode hoveredBackgroundNode;
    private readonly IconImageNode iconNode;
    private readonly TextNode nameNode;
    private readonly TextNode timeRemainingNode;
    private readonly TextNode levelNode;
    private readonly ProgressBarNode progressNode;
    private readonly TextNode progressTextNode;
    
    public FateEntryNode() {
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

        iconNode = new IconImageNode {
            NodeId = 3,
            IsVisible = true,
        };
        System.NativeController.AttachNode(iconNode, this);

        nameNode = new TextNode {
            NodeId = 4,
            IsVisible = true,
            AlignmentType = AlignmentType.BottomLeft,
            TextFlags = TextFlags.Ellipsis,
        };
        System.NativeController.AttachNode(nameNode, this);

        timeRemainingNode = new TextNode {
            NodeId = 5,
            IsVisible = true,
            AlignmentType = AlignmentType.BottomRight,
        };
        System.NativeController.AttachNode(timeRemainingNode, this);

        levelNode = new TextNode {
            NodeId = 6,
            IsVisible = true,
            AlignmentType = AlignmentType.Left,
        };
        System.NativeController.AttachNode(levelNode, this);

        progressNode = new ProgressBarNode {
            NodeId = 7,
            IsVisible = true,
        };
        progressNode.CollisionNode.EventFlagsSet = false;
        System.NativeController.AttachNode(progressNode, this);

        progressTextNode = new TextNode {
            NodeId = 8,
            IsVisible = true,
            AlignmentType = AlignmentType.Left,
        };
        System.NativeController.AttachNode(progressTextNode, this);
        
        CollisionNode.AddEvent(AddonEventType.MouseOver, _ => {
            IsHovered = true;
        });
        
        CollisionNode.AddEvent(AddonEventType.MouseClick, _ => {
            if (Fate is null) return;
            
            var agentMap = AgentMap.Instance();
            agentMap->OpenMap(agentMap->CurrentMapId, agentMap->CurrentTerritoryId);
            agentMap->FlagMarkerCount = 0;
            agentMap->SetFlagMapMarker(agentMap->CurrentTerritoryId, agentMap->CurrentMapId, Fate.Position, Fate.MapIconId);
            RaptureAtkModule.Instance()->FocusAddon(agentMap->AddonId);
        });
        
        CollisionNode.AddEvent(AddonEventType.MouseOut, _ => {
            IsHovered = false;
        });
        
        AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 120)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .AddLabel(61, 2, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(120, 0, AtkTimelineJumpBehavior.LoopForever, 2)
            .EndFrameSet()
            .Build());

        timeRemainingNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddFrame(1, multiplyColor: new Vector3(100.0f, 100.0f, 100.0f))
            .EndFrameSet()
            .BeginFrameSet(61, 120)
            .AddFrame(61, multiplyColor: new Vector3(100.0f, 100.0f, 100.0f))
            .AddFrame(80, multiplyColor: new Vector3(100.0f, 50.0f, 50.0f))
            .AddFrame(100, multiplyColor: new Vector3(100.0f, 50.0f, 50.0f))
            .AddFrame(120, multiplyColor: new Vector3(100.0f, 100.0f, 100.0f))
            .EndFrameSet()
            .Build());
        
        Timeline?.PlayAnimation(1);
    }

    public required IFate Fate {
        get;
        set {
            field = value;

            iconNode.IconId = value.MapIconId;
            nameNode.SeString = value.Name;
            timeRemainingNode.String = TimeSpan.FromSeconds(value.TimeRemaining).ToString(@"mm\:ss");

            if (Fate is not { Level: 1, MaxLevel: 255 }) {
                levelNode.String = $"Lv. {value.Level}-{value.MaxLevel}";
            }
            else {
                levelNode.String = "Lv. ???";
            }
           
            progressTextNode.String = $"{value.Progress}%";
            progressNode.Progress = value.Progress / 100.0f;
        }
    }
    
    public bool IsHovered {
        get => hoveredBackgroundNode.IsVisible;
        set => hoveredBackgroundNode.IsVisible = value;
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        hoveredBackgroundNode.Size = Size;
        
        iconNode.Position = new Vector2(2.0f, 2.0f);
        iconNode.Size = new Vector2(48.0f, 48.0f);

        progressTextNode.Size = new Vector2(50.0f, Height / 2.0f);
        progressTextNode.Position = new Vector2(Width - progressTextNode.Width, Height / 2.0f);
        
        timeRemainingNode.Size = new Vector2(50.0f, Height / 2.0f);
        timeRemainingNode.Position = new Vector2(Width - timeRemainingNode.Width, 0.0f);

        levelNode.Size = new Vector2(75.0f, Height / 2.0f);
        levelNode.Position = new Vector2(iconNode.Width + 4.0f, Height / 2.0f);
        
        progressNode.Size = new Vector2(Width - iconNode.Width - 4.0f - timeRemainingNode.Width - levelNode.Width - 4.0f, Height / 3.0f);
        progressNode.Position = new Vector2(iconNode.Width + levelNode.Width + 4.0f, Height / 2.0f + (Height / 3.0f) / 4.0f);

        nameNode.Size = new Vector2(Width - iconNode.Width - 4.0f - timeRemainingNode.Width, Height / 2.0f);
        nameNode.Position = new Vector2(iconNode.Width + 4.0f, 0.0f);
    }

    public void Update() {
        var timeRemaining = TimeSpan.FromSeconds(Fate.TimeRemaining);
        
        timeRemainingNode.String = $"{SeIconChar.Clock.ToIconChar()} {timeRemaining:mm\\:ss}";
        progressTextNode.String = $"{Fate.Progress}%";
        progressNode.Progress = Fate.Progress / 100.0f;

        if (Fate.TimeRemaining < 300 && Timeline?.ActiveLabelId is 1) {
            Timeline?.PlayAnimation(2);
        }
        else if (Fate.TimeRemaining > 300 && Timeline?.ActiveLabelId is 2) {
            Timeline?.PlayAnimation(1);
        }
    }
}
