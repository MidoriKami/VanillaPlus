using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.MSQProgressPercent;

public class MSQProgressBarConfig : GameModificationConfig<MSQProgressBarConfig> {
    protected override string FileName => "MSQProgressBar";

    public MSQProgressBarMode Mode = MSQProgressBarMode.Expansion;
    public Vector4 BarColor = KnownColor.White.Vector();
}
