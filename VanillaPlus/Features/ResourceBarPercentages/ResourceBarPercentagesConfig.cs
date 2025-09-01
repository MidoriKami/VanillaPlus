using VanillaPlus.Classes;

namespace VanillaPlus.Features.ResourceBarPercentages;

public class ResourceBarPercentagesConfig : GameModificationConfig<ResourceBarPercentagesConfig> {
    protected override string FileName => "ResourceBarPercentages.config.json";

    public bool PartyListEnabled = true;
    public bool PartyListSelfOnly = false;
    public bool ParameterWidgetEnabled = true;
    public bool PercentageSignEnabled = true;

    public int DecimalPlaces = 0;
}
