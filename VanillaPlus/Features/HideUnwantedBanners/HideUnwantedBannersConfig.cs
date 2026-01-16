using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.HideUnwantedBanners;

public class HideUnwantedBannersConfig : GameModificationConfig<HideUnwantedBannersConfig> {
    protected override string FileName =>  "HideUnwantedBanners";

    public List<BannerConfig> BannerSettings = [
        new() { BannerId = 120031, IsSuppressed = true },
        new() { BannerId = 120032, IsSuppressed = true },
        new() { BannerId = 120055, IsSuppressed = true },
        new() { BannerId = 120081, IsSuppressed = true },
        new() { BannerId = 120082, IsSuppressed = true },
        new() { BannerId = 120083, IsSuppressed = true },
        new() { BannerId = 120084, IsSuppressed = true },
        new() { BannerId = 120085, IsSuppressed = true },
        new() { BannerId = 120086, IsSuppressed = true },
        new() { BannerId = 120093, IsSuppressed = true },
        new() { BannerId = 120094, IsSuppressed = true },
        new() { BannerId = 120095, IsSuppressed = true },
        new() { BannerId = 120096, IsSuppressed = true },
        new() { BannerId = 120141, IsSuppressed = true },
        new() { BannerId = 120142, IsSuppressed = true },
        new() { BannerId = 121081, IsSuppressed = true },
        new() { BannerId = 121082, IsSuppressed = true },
        new() { BannerId = 121561, IsSuppressed = true },
        new() { BannerId = 121562, IsSuppressed = true },
        new() { BannerId = 121563, IsSuppressed = true },
        new() { BannerId = 128370, IsSuppressed = true },
        new() { BannerId = 128371, IsSuppressed = true },
        new() { BannerId = 128372, IsSuppressed = true },
        new() { BannerId = 128373, IsSuppressed = true }
    ];
}
