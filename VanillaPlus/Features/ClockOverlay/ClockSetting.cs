using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Features.ClockOverlay;

public enum ClockType { Local, Server, Eorzea }

public class ClockSetting {
    public Vector2 Position = Vector2.Zero;
    public ClockType Type = ClockType.Local;
    public bool IsMoveable = true;
    public bool ShowSeconds = true;
    public bool ShowPrefix = true;
    public TextFlags Flags = TextFlags.Edge;
}
