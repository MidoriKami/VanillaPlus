using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.HideUnwantedBanners;

public class HideUnwantedBannersConfig : GameModificationConfig<HideUnwantedBannersConfig> {
    protected override string FileName =>  "HideUnwantedBanners.config.json";

    public HashSet<int> HiddenBanners = [];
}
