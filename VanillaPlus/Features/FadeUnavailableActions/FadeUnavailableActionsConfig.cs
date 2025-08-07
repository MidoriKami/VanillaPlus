using VanillaPlus.Classes;

namespace VanillaPlus.Features.FadeUnavailableActions;

public class FadeUnavailableActionsConfig : GameModificationConfig<FadeUnavailableActionsConfig> {
    protected override string FileName => "FadeUnavailableActions.config.json";

    public int FadePercentage = 70;
    public bool ApplyToFrame = true;
    public int ReddenPercentage = 50;
    public bool ReddenOutOfRange = true;
    public bool ApplyToSyncActions = false;
}
