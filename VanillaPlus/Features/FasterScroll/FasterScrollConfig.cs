﻿using VanillaPlus.Classes;

namespace VanillaPlus.FasterScroll;

public class FasterScrollConfig : GameModificationConfig<FasterScrollConfig> {
    protected override string FileName => "FasterScroll.config.json";

    public float SpeedMultiplier = 3.0f;
}
