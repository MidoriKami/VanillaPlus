using VanillaPlus.Classes;

namespace VanillaPlus.Features.TargetCastBarCountdown;

public class TargetCastBarCountdownConfig : GameModificationConfig<TargetCastBarCountdownConfig> {
    protected override string FileName => "TargetCastBarCountdown.config.json";

    public bool PrimaryTarget = true;
    public bool FocusTarget = false;
}
