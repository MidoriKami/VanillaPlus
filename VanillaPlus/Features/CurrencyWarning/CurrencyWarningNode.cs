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

public unsafe class CurrencyWarningNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;
    private readonly IconImageNode iconNode;
    public required CurrencyWarningConfig Config { get; init; }

    public bool IsHovered { get; private set; }
    public List<(uint IconId, string Name, long Count, bool IsHigh)> ActiveWarnings { get; } = [];

    public Action? OnUpdate { get; set; }

    public CurrencyWarningNode() {
        iconNode = new IconImageNode {
            Size = new Vector2(48.0f, 48.0f),
            FitTexture = true,
            IconId = Config?.LowIcon ?? 60073u,
        };
        iconNode.AttachNode(this);
        BuildPulseAnimation();
    }

    public override void Update() {
        base.Update();
        Scale = new Vector2(Config.Scale);
        EnableMoving = Config.IsMoveable;

        ActiveWarnings.Clear();
        bool hasHigh = false;

        foreach (var setting in Config.WarningSettings) {
            var count = InventoryManager.Instance()->GetInventoryItemCount(setting.ItemId);
            bool isLow = setting.EnableLowLimit && count < setting.LowLimit;
            bool isHigh = setting.EnableHighLimit && count >= setting.HighLimit;

            if (isLow || isHigh) {
                var item = Services.DataManager.GetItem(setting.ItemId);
                ActiveWarnings.Add((item.Icon, item.Name.ToString(), count, isHigh));
                if (isHigh) hasHigh = true;
            }
        }

        bool shouldShow = ActiveWarnings.Count > 0 || Config.IsMoveable;
        IsVisible = shouldShow;

        if (ActiveWarnings.Count > 0) {
            iconNode.IconId = hasHigh ? Config.HighIcon : Config.LowIcon;
            iconNode.Timeline?.PlayAnimation(0);
        } else {
            iconNode.IconId = Config.LowIcon;
            iconNode.Timeline?.StopAnimation();
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
        var timeline = new TimelineBuilder()
            .BeginFrameSet(1, 60)
                .AddFrame(1, scale: new Vector2(1.0f, 1.0f))
                .AddFrame(30, scale: new Vector2(1.25f, 1.25f))
                .AddFrame(60, scale: new Vector2(1.0f, 1.0f))
                .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 1)
            .EndFrameSet()
            .Build();

        iconNode.AddTimeline(timeline);
    }
}
