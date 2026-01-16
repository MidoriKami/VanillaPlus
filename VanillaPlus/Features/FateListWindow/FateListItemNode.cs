using System;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Timelines;

namespace VanillaPlus.Features.FateListWindow;

public class FateListItemNode : ListItemNode<IFate> {
    private readonly IconImageNode iconNode;
    private readonly TextNode nameNode;
    private readonly TextNode timeRemainingNode;
    private readonly TextNode levelNode;
    private readonly ProgressBarNode progressNode;
    private readonly TextNode progressTextNode;
    
    public FateListItemNode() {
        iconNode = new IconImageNode {
            FitTexture = true,
        };
        iconNode.AttachNode(this);

        nameNode = new TextNode {
            AlignmentType = AlignmentType.BottomLeft,
            TextFlags = TextFlags.Ellipsis,
        };
        nameNode.AttachNode(this);

        timeRemainingNode = new TextNode {
            AlignmentType = AlignmentType.BottomRight,
        };
        timeRemainingNode.AttachNode(this);

        levelNode = new TextNode {
            AlignmentType = AlignmentType.Left,
        };
        levelNode.AttachNode(this);

        progressNode = new ProgressBarNode {
            DisableCollisionNode = true,
        };
        progressNode.AttachNode(this);

        progressTextNode = new TextNode {
            AlignmentType = AlignmentType.Left,
        };
        progressTextNode.AttachNode(this);

        CollisionNode.AddEvent(AtkEventType.MouseClick, () => ItemData?.FocusMarker());
        
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

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
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

    public override void Update() {
        if (ItemData is null) return;
        
        if (ItemData.TimeRemainingSpan > TimeSpan.Zero) {
            timeRemainingNode.String = $"{SeIconChar.Clock.ToIconChar()} {ItemData.TimeRemainingString}";
            
            if (ItemData.TimeRemaining < 300 && Timeline?.ActiveLabelId is 1) {
                Timeline?.PlayAnimation(2);
            }
            else if (ItemData.TimeRemaining > 300 && Timeline?.ActiveLabelId is 2) {
                Timeline?.PlayAnimation(1);
            }
        }
        else {
            timeRemainingNode.String = "Pending";
            if (Timeline?.ActiveLabelId is 2) {
                Timeline?.PlayAnimation(1);
            }
        }
        
        progressTextNode.String = $"{ItemData.Progress}%";
        progressNode.Progress = ItemData.Progress / 100.0f;
    }

    public override float ItemHeight => 53.0f;

    protected override void SetNodeData(IFate itemData) {
        iconNode.IconId = itemData.MapIconId;
        nameNode.SeString = itemData.NameString;
        timeRemainingNode.String = itemData.TimeRemainingString;

        if (ItemData is not { Level: 1, MaxLevel: 255 }) {
            levelNode.String = Strings.FateEntry_LevelRangeFormat.Format(itemData.Level, itemData.MaxLevel);
        }
        else {
            levelNode.String = Strings.FateEntry_LevelUnknown;
        }
           
        progressTextNode.String = $"{itemData.Progress}%";
        progressNode.Progress = itemData.Progress / 100.0f;
    }
}
