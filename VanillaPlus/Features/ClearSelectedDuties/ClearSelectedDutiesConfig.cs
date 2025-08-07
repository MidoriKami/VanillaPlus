using VanillaPlus.Classes;

namespace VanillaPlus.Features.ClearSelectedDuties;

public class ClearSelectedDutiesConfig : GameModificationConfig<ClearSelectedDutiesConfig> {
    protected override string FileName => "ClearSelectedDuties.config.json";

    public bool DisableWhenUnrestricted = true;
}
