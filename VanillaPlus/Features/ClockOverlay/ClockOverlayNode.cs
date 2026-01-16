using System;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ClockOverlay;

public class ClockOverlayNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    private readonly TextNode timeNode;
    private readonly ClockOverlayConfig config;

    public ClockOverlayNode(ClockOverlayConfig config) {
        this.config = config;

        timeNode = new TextNode();
        timeNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        timeNode.Size = Size;
    }

    protected override void OnUpdate() {
        timeNode.TextFlags = config.TextFlags;
        timeNode.TextColor = config.TextColor;
        timeNode.TextOutlineColor = config.TextOutlineColor;
        timeNode.FontSize = (uint)config.FontSize;
        timeNode.FontType = config.FontType;
        timeNode.AlignmentType = config.AlignmentType;
        
        EnableMoving = config.IsMoveable;

        var format = config.ShowSeconds ? "HH:mm:ss" : "HH:mm";
        var prefix = config.ShowPrefix ? GetPrefix(config.Type) : string.Empty;

        timeNode.String = config.Type switch {
            ClockType.Local => $"{prefix}{DateTime.Now.ToString(format)}",
            ClockType.Server => $"{prefix}{GetServerTime().ToString(format)}",
            ClockType.Eorzea => $"{prefix}{GetEorzeaTime():HH:mm}",
            _ => "00:00",
        };
    }

    private static DateTime GetServerTime()
        => DateTimeOffset.FromUnixTimeSeconds(Framework.GetServerTime()).DateTime;

    private static DateTime GetEorzeaTime() {
        const double eorzeaMultiplier = 3600.0D / 175.0D;
        var eorzeaTotalSeconds = (long)(Framework.GetServerTime() * eorzeaMultiplier);

        var hour = (int)(eorzeaTotalSeconds / 3600 % 24);
        var minute = (int)(eorzeaTotalSeconds / 60 % 60);
        var seconds = (int)(eorzeaTotalSeconds % 60);
        
        return new DateTime(1, 1, 1, hour, minute, seconds);
    }

    private string GetPrefix(ClockType type) => type switch {
        ClockType.Local => timeNode.FontType is FontType.Axis ? " " : "LT ",
        ClockType.Server => timeNode.FontType is FontType.Axis ? " " : "ST ",
        ClockType.Eorzea => timeNode.FontType is FontType.Axis ? " " : "ET ",
        _ => string.Empty,
    };
}
