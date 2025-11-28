using VanillaPlus.Classes;

namespace VanillaPlus.Features.ClearSelectedDuties;

public class ClearSelectedDutiesConfig : GameModificationConfig<ClearSelectedDutiesConfig> {
    protected override string FileName => "ClearSelectedDuties";

    public bool DisableWhenUnrestricted = true;
}
