using System;
using System.Drawing;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface;

namespace VanillaPlus.Features.WindowBackground;

public class WindowBackgroundSetting {
    public const string InvalidName = "Window not Set";

    public string AddonName = InvalidName;
    public Vector4 Color = KnownColor.Black.Vector() with { W = 50.0f };
    public Vector2 Padding = new(30.0f, 30.0f);

    public static bool IsMatch(WindowBackgroundSetting itemData, string searchString) {
        var regex = new Regex(searchString, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        return regex.IsMatch(itemData.AddonName);
    }

    public static int Compare(WindowBackgroundSetting left, WindowBackgroundSetting right, string mode)
        => string.Compare(left.AddonName, right.AddonName, StringComparison.OrdinalIgnoreCase);
}
