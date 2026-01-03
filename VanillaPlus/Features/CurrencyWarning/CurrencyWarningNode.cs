using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Classes.Timelines;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using VanillaPlus.Features.CurrencyOverlay;

namespace VanillaPlus.Features.CurrencyWarning;

public unsafe class CurrencyWarningNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    private readonly IconImageNode iconNode;
    private readonly CurrencyWarningConfig config;
    private readonly CurrencyOverlayConfig currencyOverlayConfig;

    public CurrencyWarningNode(CurrencyWarningConfig config, CurrencyOverlayConfig currencyOverlayConfig) {
        this.config = config;
        this.currencyOverlayConfig = currencyOverlayConfig;

        iconNode = new IconImageNode {
            Size = new Vector2(48.0f, 48.0f),
            FitTexture = true,
            IconId = 60071,
        };
        iconNode.AttachNode(this);

        BuildPulseAnimation();
    }

    public override void Update() {
        base.Update();

        Scale = new Vector2(config.Scale);
        EnableMoving = config.IsMoveable;

        var sb = new StringBuilder();
        bool hasWarning = false;
        bool hasLimitReached = false;

        foreach (var currency in currencyOverlayConfig.Currencies) {
            var count = InventoryManager.Instance()->GetInventoryItemCount(currency.ItemId);

            bool low = currency.EnableLowLimit && count < currency.LowLimit;
            bool high = currency.EnableHighLimit && count > currency.HighLimit;

            if (low || high) {
                var item = Services.DataManager.GetItem(currency.ItemId);
                hasWarning = true;
                if (high) hasLimitReached = true;

                sb.AppendLine($"{item.Name}: {count} {(high ? "[MAX]" : "[LOW]")}");
            }
        }

        IsVisible = hasWarning || config.IsMoveable;
        if (!IsVisible) return;

        iconNode.IconId = hasLimitReached ? (uint)60074 : (uint)60073;
        TextTooltip = $"Currency Notification:\n{sb.ToString().TrimEnd()}";
    }

    private void BuildPulseAnimation() {
        var pulseTimeline = new TimelineBuilder()
            .BeginFrameSet(1, 60)
            .AddFrame(1, scale: new Vector2(1.0f, 1.0f))
            .AddFrame(30, scale: new Vector2(1.2f, 1.2f))
            .AddFrame(60, scale: new Vector2(1.0f, 1.0f))
            .AddLabel(60, 0, AtkTimelineJumpBehavior.LoopForever, 0)
            .EndFrameSet()
            .Build();

        iconNode.AddTimeline(pulseTimeline);
        iconNode.Timeline?.PlayAnimation(0);
    }
}
