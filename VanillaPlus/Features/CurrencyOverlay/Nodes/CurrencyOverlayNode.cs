using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using KamiToolKit.Timelines;

namespace VanillaPlus.Features.CurrencyOverlay.Nodes;

public unsafe class CurrencyOverlayNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    private readonly IconImageNode iconImageNode;
    private readonly CounterNode countNode;

    public CurrencyOverlayNode() {
        iconImageNode = new IconImageNode {
            FitTexture = true,
        };
        iconImageNode.AttachNode(this);

        countNode = new CounterNode {
            NumberWidth = 10,
            CommaWidth = 8,
            SpaceWidth = 6,
            TextAlignment = AlignmentType.Right,
            CounterWidth = 104.0f,
            Font = CounterFont.MoneyFont,
        };
        countNode.AttachNode(this);

        BuildTimelines();
    }

    private void BuildTimelines() {
        AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 120)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .AddLabel(61, 2, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(120, 0, AtkTimelineJumpBehavior.LoopForever, 2)
            .EndFrameSet()
            .Build());
        
        countNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddFrame(1, scale: new Vector2(1.0f, 1.0f), addColor: new Vector3(0.0f, 0.0f, 0.0f))
            .AddFrame(30, scale: new Vector2(1.075f, 1.075f), addColor: new Vector3(100.0f, 0.0f, 0.0f))
            .AddFrame(60, scale: new Vector2(1.0f, 1.0f), addColor: new Vector3(0.0f, 0.0f, 0.0f))
            .EndFrameSet()
            .BeginFrameSet(61, 120)
            .AddFrame(61, scale: new Vector2(1.0f, 1.0f), addColor: new Vector3(0.0f, 0.0f, 0.0f))
            .EndFrameSet()
            .Build());
    }

    public required CurrencySetting Currency {
        get;
        init {
            field = value;
            iconImageNode.IconId = Services.DataManager.GetItem(Currency.ItemId).Icon;

            countNode.Size = new Vector2(128.0f, 22.0f);
            countNode.Origin = countNode.Size / 2.0f;
            iconImageNode.Size = new Vector2(36.0f, 36.0f);
        }
    }

    protected override void OnUpdate() {
        var inventoryCount = InventoryManager.Instance()->GetInventoryItemCount(Currency.ItemId);

        countNode.Number = inventoryCount;

        EnableMoving = Currency.IsNodeMoveable;

        if (Currency.IconReversed) {
            iconImageNode.Position = new Vector2(0.0f, 0.0f);
            countNode.Position = new Vector2(iconImageNode.X + iconImageNode.Width, 8.0f);
        }
        else {
            countNode.Position = new Vector2(0.0f, 8.0f);
            iconImageNode.Position = new Vector2(countNode.X + countNode.Width, 0.0f);
        }

        countNode.TextAlignment = Currency.TextReversed ? AlignmentType.Left : AlignmentType.Right;

        Scale = new Vector2(Currency.Scale, Currency.Scale);

        var isLowWarning = Currency.EnableLowLimit && inventoryCount <= Currency.LowLimit;
        var isHighWarning = Currency.EnableHighLimit && inventoryCount >= Currency.HighLimit;
        var hasWarning = isLowWarning || isHighWarning;

        var shouldFade = Currency.FadeIfNoWarnings && !hasWarning;
        var alpha = shouldFade ? 1.0f - Currency.FadePercent : 1.0f;
        iconImageNode.Alpha = alpha;
        countNode.Alpha = alpha;

        if (hasWarning) { 
            Timeline?.PlayAnimation(1);
        }
        else { 
            Timeline?.PlayAnimation(2);
        }
    }
}
