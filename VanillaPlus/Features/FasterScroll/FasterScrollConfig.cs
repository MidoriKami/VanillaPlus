using VanillaPlus.Classes;

namespace VanillaPlus.Features.FasterScroll;

public class FasterScrollConfig : GameModificationConfig<FasterScrollConfig> {
    protected override string FileName => "FasterScroll";

    public float SpeedMultiplier = 3.0f;
}
