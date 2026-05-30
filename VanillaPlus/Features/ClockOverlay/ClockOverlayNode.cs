using System;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Overlay.UiOverlay;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ClockOverlay;

public class ClockOverlayNode : OverlayNode {
    public override OverlayLayer OverlayLayer => OverlayLayer.BehindUserInterface;

    private readonly TextNode timeNode;
    public required ClockOverlayConfig Config { get; init; }

    public ClockOverlayNode() {
        timeNode = new TextNode();
        timeNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        timeNode.Size = Size;
    }

    protected override void OnUpdate() {
        timeNode.TextFlags = Config.TextFlags;
        timeNode.TextColor = Config.TextColor;
        timeNode.TextOutlineColor = Config.TextOutlineColor;
        timeNode.FontSize = (uint)Config.FontSize;
        timeNode.FontType = Config.FontType;
        timeNode.AlignmentType = Config.AlignmentType;

        EnableMoving = Config.IsMoveable;

        var format = Config.ShowSeconds ? "HH:mm:ss" : "HH:mm";
        var prefix = Config.ShowPrefix ? GetPrefix(Config.Type) : string.Empty;

        timeNode.String = Config.Type switch {
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
