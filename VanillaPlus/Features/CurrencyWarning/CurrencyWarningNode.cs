using System;
using System.Collections.Generic;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.Timelines;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;

namespace VanillaPlus.Features.CurrencyWarning;

public record WarningInfo(uint IconId, string Name, long Count, bool IsHigh, int Limit);

public unsafe class CurrencyWarningNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
    private readonly IconImageNode iconNode;
    public required CurrencyWarningConfig Config { get; init; }

    public bool IsHovered { get; private set; }
    public List<WarningInfo> ActiveWarnings { get; } = [];

    public Action? OnUpdate { get; set; }

    public CurrencyWarningNode() {
        iconNode = new IconImageNode {
            Size = new Vector2(48.0f, 48.0f),
            FitTexture = true,
            IconId = Config?.LowIcon ?? 60073u,
            Origin = new Vector2(24.0f, 24.0f),
        };
        iconNode.AttachNode(this);
        BuildPulseAnimation();
    }

    public override void Update() {
        base.Update();
        Scale = new Vector2(Config.Scale);
        EnableMoving = Config.IsMoveable;

        ActiveWarnings.Clear();
        var hasHigh = false;

        foreach (var setting in Config.WarningSettings) {
            var count = InventoryManager.Instance()->GetInventoryItemCount(setting.ItemId);
            var isLow = setting.EnableLowLimit && count < setting.LowLimit;
            var isHigh = setting.EnableHighLimit && count >= setting.HighLimit;

            if (isLow || isHigh) {
                var item = Services.DataManager.GetItem(setting.ItemId);
                ActiveWarnings.Add(new WarningInfo(item.Icon, item.Name.ToString(), count, isHigh, isLow ? setting.LowLimit : setting.HighLimit));
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

        OnUpdate?.Invoke();
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
            .AddFrame(1, rotation: MathF.PI * 2.0f)
            .AddFrame(5, rotation: MathF.PI * 2.0f - MathF.PI / 12.0f)
            .AddFrame(10, rotation: MathF.PI * 2.0f + MathF.PI / 12.0f)
            .AddFrame(15, rotation: MathF.PI * 2.0f - MathF.PI / 12.0f)
            .AddFrame(20, rotation: MathF.PI * 2.0f)
            .AddFrame(30, rotation: MathF.PI * 2.0f)
            
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
