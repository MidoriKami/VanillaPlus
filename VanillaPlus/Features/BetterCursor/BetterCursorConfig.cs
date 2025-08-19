using System.Numerics;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.BetterCursor;

public class BetterCursorConfig : GameModificationConfig<BetterCursorConfig> {
    protected override string FileName => "BetterCursor.config.json";

    public bool Animations = true;
    public Vector4 Color = Vector4.One;
    public bool HideOnCameraMove = true;
    public float Size = 96.0f;
    public uint IconId = 60498;

    public bool OnlyShowInCombat = false;
    public bool OnlyShowInDuties = false;
}
