using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using KamiToolKit.Premade;

namespace VanillaPlus.Features.WindowBackground;

public class WindowBackgroundSetting : IInfoNodeData {
    public const string InvalidName = "Window not Set";

    public string AddonName { get; set; } = InvalidName;
    public Vector4 Color { get; set; } = KnownColor.Black.Vector() with { W = 66.0f };
    public Vector2 Padding { get; set; } = new(30.0f, 30.0f);

    public string GetLabel() 
        => AddonName;

    public string? GetSubLabel()
        => null;

    public uint? GetId()
        => null;

    public uint? GetIconId()
        => AddonName == InvalidName ? (uint) 5 : 61483;

    public string? GetTexturePath()
        => null;

    public int Compare(IInfoNodeData other, string sortingMode)
        => string.CompareOrdinal(AddonName, ((WindowBackgroundSetting)other).AddonName);
}
