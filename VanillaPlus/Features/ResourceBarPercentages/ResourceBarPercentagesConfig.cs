using VanillaPlus.Classes;

namespace VanillaPlus.Features.ResourceBarPercentages;

public class ResourceBarPercentagesConfig : GameModificationConfig<ResourceBarPercentagesConfig> {
    protected override string FileName => "ResourceBarPercentages.config.json";

    public bool PartyListEnabled = true;
    public bool PartyListSelf = true;
    public bool PartyListOtherMembers = true;

    public bool ParameterWidgetEnabled = true;
    public bool ParameterHpEnabled = true;
    public bool ParameterMpEnabled = true;
    public bool ParameterGpEnabled = true;
    public bool ParameterCpEnabled = true;

    public bool PercentageSignEnabled = true;
    public int DecimalPlaces = 0;
}
