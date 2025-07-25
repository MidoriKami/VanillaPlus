﻿using VanillaPlus.Core;

namespace VanillaPlus.TargetCastBarCountdown;

public class TargetCastBarCountdownConfig : GameModificationConfig<TargetCastBarCountdownConfig> {
    protected override string FileName => "TargetCastBarCountdown.config.json";

    public bool PrimaryTarget = true;
    public bool FocusTarget = false;
}
