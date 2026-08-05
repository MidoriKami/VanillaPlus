using System;
using System.Drawing;
using System.Numerics;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;
using KamiToolKit.Timelines;
using FateState = Dalamud.Game.ClientState.Fates.FateState;

namespace VanillaPlus.Features.FateListWindow;

public class FateListItemNode : ListItemNode<IFate>, IListItemNode {
    public static float ItemHeight => 53.0f;

    private readonly NineGridNode activeBackgroundNode;
    private readonly IconImageNode iconNode;
    private readonly IconImageNode expBonusIconNode;
    private readonly TextNode nameNode;
    private readonly TextNode timeRemainingNode;
    private readonly TextNode levelNode;
    private readonly ProgressBarNode progressNode;
    private readonly TextNode progressTextNode;

    public FateListItemNode() {
        activeBackgroundNode = new SimpleNineGridNode {
            TexturePath = "ui/uld/ListItemA.tex",
            TextureCoordinates = new Vector2(0.0f, 22.0f),
            TextureSize = new Vector2(64.0f, 22.0f),
            TopOffset = 6,
            BottomOffset = 6,
            LeftOffset = 16,
            RightOffset = 1,
            IsVisible = false,
            Color = KnownColor.Orange.Vector(),
        };
        activeBackgroundNode.AttachNode(this);

        iconNode = new IconImageNode {
            FitTexture = true,
        };
        iconNode.AttachNode(this);

        expBonusIconNode = new IconImageNode {
            FitTexture = true,
            IconId = 60934,
        };
        expBonusIconNode.AttachNode(this);

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

        progressNode = new ProgressBarNode();
        progressNode.AttachNode(this);

        progressTextNode = new TextNode {
            AlignmentType = AlignmentType.Left,
        };
        progressTextNode.AttachNode(this);

        AddEvent(AtkEventType.MouseClick, () => ItemData?.FocusMarker());

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
        activeBackgroundNode.Size = Size + new Vector2(6.0f, 6.0f);
        activeBackgroundNode.Position = new Vector2(-3.0f, -3.0f);

        iconNode.Position = new Vector2(2.0f, 2.0f);
        iconNode.Size = new Vector2(48.0f, 48.0f);

        expBonusIconNode.Position = iconNode.Position + new Vector2(iconNode.Width / 4.0f, -iconNode.Height / 4.0f);
        expBonusIconNode.Size = iconNode.Size;

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

    protected override void SetNodeData(IFate itemData) {
        unsafe {
            var fateManager = FateManager.Instance();
            activeBackgroundNode.IsVisible = fateManager->CurrentFate != null && fateManager->CurrentFate->FateId == itemData.FateId;
        }

        iconNode.IconId = itemData.MapIconId;
        nameNode.String = itemData.NameString;

        expBonusIconNode.IsVisible = itemData.HasBonus;

        switch (itemData.State) {
            case FateState.Preparing:
            case FateState.Running when itemData.TimeRemainingSpan <= TimeSpan.Zero:
                timeRemainingNode.String = "Pending";
                Timeline?.PlayAnimation(1);
                break;

            case FateState.Running:
                timeRemainingNode.String = $"{SeIconChar.Clock.ToIconChar()} {itemData.TimeRemainingString}";
                Timeline?.PlayAnimation(itemData.TimeRemaining < 300 ? 2 : 1);
                break;
        }

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
