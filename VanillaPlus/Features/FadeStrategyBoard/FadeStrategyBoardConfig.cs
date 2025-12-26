using VanillaPlus.Classes;

namespace VanillaPlus.Features.FadeStrategyBoard;

public class FadeStrategyBoardConfig : GameModificationConfig<FadeStrategyBoardConfig> {
    protected override string FileName => "FadeStrategyBoard";

    public float FadePercentage = 0.8f;
}
