using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ConfigSearchBar;

public class ConfigSearchBarConfig : GameModificationConfig<ConfigSearchBarConfig> {
    protected override string FileName => "ConfigSearchBar";

    public Vector4 TabColor = KnownColor.LimeGreen.Vector();
    public Vector4 HighlightColor = KnownColor.MediumVioletRed.Vector();
}
