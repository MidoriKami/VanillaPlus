using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using KamiToolKit.Timelines;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.CurrencyWarning.Nodes;

public record WarningInfo(uint IconId, string Name, long Count, bool IsHigh, int Limit);

public unsafe class CurrencyWarningOverlayNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
    private readonly IconImageNode iconNode;
    public required CurrencyWarningConfig Config { get; init; }

    public bool IsHovered { get; private set; }
    public List<WarningInfo> ActiveWarnings { get; } = [];

    public required CurrencyTooltipNode TooltipNode { get; init; }

    public CurrencyWarningOverlayNode() {
        iconNode = new IconImageNode {
            Size = new Vector2(48.0f, 48.0f),
            FitTexture = true,
            IconId = Config?.LowIcon ?? 60073u,
            Origin = new Vector2(24.0f, 24.0f),
        };
        iconNode.AttachNode(this);
        BuildPulseAnimation();
    }

    protected override void OnUpdate() {
        Scale = new Vector2(Config.Scale);
        EnableMoving = Config.IsMoveable;

        ActiveWarnings.Clear();
        var hasHigh = false;

        foreach (var setting in Config.WarningSettings) {
            var count = InventoryManager.Instance()->GetInventoryItemCount(setting.ItemId);
            var isLow = setting.Mode == WarningMode.Below && count < setting.Limit;
            var isHigh = setting.Mode == WarningMode.Above && count >= setting.Limit;

            if (isLow || isHigh) {
                var item = Services.DataManager.GetItem(setting.ItemId);
                ActiveWarnings.Add(new WarningInfo(item.Icon, item.Name.ToString(), count, isHigh, setting.Limit));
                if (isHigh) hasHigh = true;
            }
        }

        var shouldShow = ActiveWarnings.Count > 0 || Config.IsMoveable;
        IsVisible = shouldShow && !(Services.Condition.IsBoundByDuty || Services.Condition.IsInCutsceneOrQuestEvent);
        
        if (ActiveWarnings.Count > 0) {
            iconNode.IconId = hasHigh ? Config.HighIcon : Config.LowIcon;
            Timeline?.PlayAnimation(Config.PlayAnimations ? 1 : 2);
        } else {
            iconNode.IconId = Config.LowIcon;
            Timeline?.StopAnimation();
        }

        ref var cursor = ref UIInputData.Instance()->CursorInputs;
        var cursorPos = new Vector2(cursor.PositionX, cursor.PositionY);
        var screenPos = Position;
        var size = iconNode.Size * Scale;

        IsHovered = IsVisible &&
                    cursorPos.X >= screenPos.X && cursorPos.X <= screenPos.X + size.X &&
                    cursorPos.Y >= screenPos.Y && cursorPos.Y <= screenPos.Y + size.Y;

        HandleWarningUpdate();
    }

    private void HandleWarningUpdate() {
        if (IsHovered && ActiveWarnings.Count > 0) {
            TooltipNode.UpdateContents(ActiveWarnings);
            TooltipNode.IsVisible = true;
            UpdateTooltipPosition();
        } else {
            TooltipNode.IsVisible = false;
        }
    }

    private void UpdateTooltipPosition() {
        var screenSize = (Vector2)AtkStage.Instance()->ScreenSize;
        var iconScale = Scale.X;
        var iconSize = Size * iconScale;
        var tooltipSize = TooltipNode.Size;

        var targetX = Position.X + iconSize.X + 10.0f;
        var targetY = Position.Y;

        if (targetX + tooltipSize.X > screenSize.X) {
            targetX = Position.X - tooltipSize.X - 10.0f;
        }

        if (targetY + tooltipSize.Y > screenSize.Y) {
            targetY = screenSize.Y - tooltipSize.Y - 10.0f;
        }

        if (targetY < 0) targetY = 10.0f;

        TooltipNode.Position = new Vector2(targetX, targetY);
    }
    
    private void BuildPulseAnimation() {
        AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddLabel(1, 1, AtkTimelineJumpBehavior.Start, 0) // Label 1: Pulsing Animation
            .AddLabel(30, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .AddLabel(31, 2, AtkTimelineJumpBehavior.Start, 0) // Label 2: No Animation
            .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 2)
            .EndFrameSet()
            .Build());
        
        iconNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 30)
            .AddFrame(1, rotationDegrees: 0.0f)
            .AddFrame(5, rotationDegrees: 25.0f)
            .AddFrame(10, rotationDegrees: -25.0f)
            .AddFrame(15, rotationDegrees: 25.0f)
            .AddFrame(20, rotationDegrees: 0.0f)
            .AddFrame(30, rotationDegrees: 0.0f)

            .AddFrame(1, scale: new Vector2(0.95f, 0.95f), alpha: 175)
            .AddFrame(10, scale: new Vector2(0.95f, 0.95f), alpha: 175)
            .AddFrame(15, scale: new Vector2(1.0f, 1.0f), alpha: 255)
            .AddFrame(25, scale: new Vector2(0.95f, 0.95f), alpha: 175)
            .AddFrame(30, scale: new Vector2(0.95f, 0.95f), alpha: 175)

            .EndFrameSet()
            .BeginFrameSet(31, 60)
            .AddFrame(31, alpha: 255, scale: new Vector2(1.0f, 1.0f), rotation: MathF.PI * 2.0f)
            .EndFrameSet()
            .Build());
    }
}
