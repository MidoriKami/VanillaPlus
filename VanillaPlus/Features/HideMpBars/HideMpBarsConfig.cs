using VanillaPlus.Classes;

namespace VanillaPlus.Features.HideMpBars;

public class HideMpBarsConfig : GameModificationConfig<HideMpBarsConfig> {
    protected override string FileName => "HideMpBars";

    public bool HidePartyList = true;
    public bool HideParamWidget;
}
