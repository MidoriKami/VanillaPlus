using VanillaPlus.Classes;

namespace VanillaPlus.Features.ResourceBarPercentages;

public class ResourceBarPercentagesConfig : GameModificationConfig<ResourceBarPercentagesConfig> {
    protected override string FileName => "ResourceBarPercentages.config.json";

    public bool PartyListEnabled = true;
    public bool PartyListSelf = true;
    public bool PartyListMembers = true;
    public bool PartyListHpEnabled = true;
    public bool PartyListMpEnabled = true;
    public bool PartyListGpEnabled = true;
    public bool PartyListCpEnabled = true;

    public bool ParameterWidgetEnabled = true;
    public bool ParameterHpEnabled = true;
    public bool ParameterMpEnabled = true;
    public bool ParameterGpEnabled = true;
    public bool ParameterCpEnabled = true;

    public bool PercentageSignEnabled = true;
    public int DecimalPlaces = 0;
    public bool ShowDecimalsBelowHundredOnly = false;
}
