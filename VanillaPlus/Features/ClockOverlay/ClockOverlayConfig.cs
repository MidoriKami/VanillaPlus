using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ClockOverlay;

public class ClockOverlayConfig : GameModificationConfig<ClockOverlayConfig> {
    protected override string FileName => "ClockOverlay";

    public ClockSetting Clock = new();

    public bool ShowSeconds { get => Clock.ShowSeconds; set => Clock.ShowSeconds = value; }
    public bool IsMoveable { get => Clock.IsMoveable; set => Clock.IsMoveable = value; }
    public bool ShowPrefix { get => Clock.ShowPrefix; set => Clock.ShowPrefix = value; }
    public ClockType Type { get => Clock.Type; set => Clock.Type = value; }
    public TextFlags Flags { get => Clock.Flags; set => Clock.Flags = value; }
}
