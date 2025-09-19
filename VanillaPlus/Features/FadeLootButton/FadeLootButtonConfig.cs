using VanillaPlus.Classes;

namespace VanillaPlus.Features.FadeLootButton;

public class FadeLootButtonConfig : GameModificationConfig<FadeLootButtonConfig> {
    protected override string FileName => "FadeLootButton.config.json";

    public float FadePercent = 0.5f;
}
