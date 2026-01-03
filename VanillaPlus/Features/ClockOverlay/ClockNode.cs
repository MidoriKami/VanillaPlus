using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using VanillaPlus.NativeElements.Config.NodeEntries;

namespace VanillaPlus.Features.ClockOverlay;

public class ClockNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    private readonly TextNode timeNode;
    private readonly ClockOverlayConfig config;
    private readonly TextNodeStyle style;

    public ClockNode(ClockOverlayConfig config, TextNodeStyle style) {
        this.config = config;
        this.style = style;

        style.ApplyStyle(timeNode);

        timeNode = new TextNode {
            TextFlags = TextFlags.Edge,
        };
        timeNode.AttachNode(this);
    }

    public override void Update() {
        base.Update();

        style.ApplyStyle(timeNode);

        timeNode.TextFlags = config.Flags;

        timeNode.Position = Vector2.Zero;

        var format = config.ShowSeconds ? "HH:mm:ss" : "HH:mm";
        var prefix = config.ShowPrefix ? GetPrefix(config.Type) : string.Empty;

        timeNode.String = config.Type switch {
            ClockType.Local => $"{prefix}{DateTime.Now.ToString(format)}",
            ClockType.Server => $"{prefix}{GetServerTime().ToString(format)}",
            ClockType.Eorzea => $"{prefix}{GetEorzeaTime():HH:mm}",
            _ => "00:00"
        };

        EnableMoving = config.IsMoveable;
    }

    private DateTime GetServerTime() => DateTimeOffset.FromUnixTimeSeconds(Framework.GetServerTime()).LocalDateTime;

    private DateTime GetEorzeaTime() {
        const double eorzeaMultiplier = 3600.0D / 175.0D;
        long eorzeaTotalSeconds = (long)(Framework.GetServerTime() * eorzeaMultiplier);
        return new DateTime(1, 1, 1, (int)(eorzeaTotalSeconds / 3600 % 24), (int)(eorzeaTotalSeconds / 60 % 60), (int)(eorzeaTotalSeconds % 60));
    }

    private string GetPrefix(ClockType type) => type switch {
        ClockType.Local => style.FontType == FontType.Axis ? " " : "LT ",
        ClockType.Server => style.FontType == FontType.Axis ? " " : "ST ",
        ClockType.Eorzea => style.FontType == FontType.Axis ? " " : "ET ",
        _ => ""
    };
}
