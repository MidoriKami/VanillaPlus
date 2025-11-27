using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.Timelines;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;

namespace VanillaPlus.Features.BetterCursor;

public unsafe class CursorImageNode : OverlayNode {

    public override OverlayLayer OverlayLayer => OverlayLayer.Foreground;
    public override bool HideWithNativeUi => false;

    public required BetterCursorConfig Config { get; set; }

    private readonly IconImageNode imageNode;

    public CursorImageNode() {
        imageNode = new IconImageNode {
            IconId = 60498,
            FitTexture = true,
        };
        imageNode.AttachNode(this);
        
        AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 120)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .AddLabel(61, 2, AtkTimelineJumpBehavior.Start, 0)
            .AddLabel(120, 0, AtkTimelineJumpBehavior.LoopForever, 2)
            .EndFrameSet()
            .Build());

        imageNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddFrame(1, scale: new Vector2(1.0f, 1.0f))
            .AddFrame(30, scale: new Vector2(0.75f, 0.75f))
            .AddFrame(60, scale: new Vector2(1.0f, 1.0f))
            .EndFrameSet()
            .BeginFrameSet(61, 120)
            .AddFrame(61, scale: new Vector2(1.0f, 1.0f))
            .EndFrameSet()
            .Build());
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        imageNode.Size = Size;
        imageNode.Origin = new Vector2(Config.Size / 2.0f);
    }

    public override void Update() {
        base.Update();
        
        Size = new Vector2(Config.Size);

        imageNode.Color = Config.Color;
        imageNode.IconId = Config.IconId;
        
        Timeline?.PlayAnimation(Config.Animations ? 1 : 2);
        
        ref var cursorData = ref UIInputData.Instance()->CursorInputs;
        Position = new Vector2(cursorData.PositionX, cursorData.PositionY) - imageNode.Size / 2.0f;

        var isLeftHeld = (cursorData.MouseButtonHeldFlags & MouseButtonFlags.LBUTTON) != 0;
        var isRightHeld = (cursorData.MouseButtonHeldFlags & MouseButtonFlags.RBUTTON) != 0;

        if (Config is { OnlyShowInCombat: true } or { OnlyShowInDuties: true }) {
            var shouldShow = true;
            shouldShow &= !Config.OnlyShowInCombat || Services.Condition.IsInCombat();
            shouldShow &= !Config.OnlyShowInDuties || Services.Condition.IsBoundByDuty();
            shouldShow &= !Config.HideOnCameraMove || (!isLeftHeld && !isRightHeld);

            IsVisible = shouldShow;
        }
        else {
            IsVisible = !isLeftHeld && !isRightHeld || !Config.HideOnCameraMove;
        }
    }
}
