using System.Drawing;
using System.Numerics;
using Dalamud.Interface;

namespace VanillaPlus.Features.WindowBackground;

public class WindowBackgroundSetting {
    public required string AddonName = string.Empty;
    public Vector4 Color = KnownColor.Black.Vector() with { W = 0.50f };
    public Vector4 PaddingVector = new(30.0f, 30.0f, 30.0f, 30.0f);
}
