using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ClockOverlay;

public class ClockOverlayConfig : GameModificationConfig<ClockOverlayConfig> {
    protected override string FileName => "ClockOverlay";

    public Vector2 Position = Vector2.Zero;
    public ClockType Type = ClockType.Local;
    public bool IsMoveable = true;
    public bool ShowSeconds = true;
    public bool ShowPrefix = true;
    public TextFlags Flags = TextFlags.Edge;
}
