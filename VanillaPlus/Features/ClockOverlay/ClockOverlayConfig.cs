using System.Numerics;
using System.Text.Json.Serialization;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ClockOverlay;

public unsafe class ClockOverlayConfig : GameModificationConfig<ClockOverlayConfig> {
    protected override string FileName => "ClockOverlay";

    public Vector2 Position = (Vector2)AtkStage.Instance()->ScreenSize / 2.0f - new Vector2(150.0f, 30.0f) / 2.0f;
    public ClockType Type = ClockType.Local;
    public bool ShowSeconds = true;
    public bool ShowPrefix = true;
    
    public TextFlags TextFlags = TextFlags.Edge;
    public Vector4 TextColor = ColorHelper.GetColor(1);
    public Vector4 TextOutlineColor = ColorHelper.GetColor(54);
    public int FontSize = 20;
    public FontType FontType = FontType.Axis;
    public AlignmentType AlignmentType = AlignmentType.Center;
    
    [JsonIgnore] public bool IsMoveable = false;
}
