using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.HideUnwantedBanners;

public class HideUnwantedBannersConfig : GameModificationConfig<HideUnwantedBannersConfig> {
    protected override string FileName =>  "HideUnwantedBanners.config.json";

    public HashSet<uint> HiddenBanners = [];
    
    public HashSet<uint> SeenBanners = [
        120031, 120032, 120055, 120081, 120082, 120083, 120084,
        120085, 120086, 120093, 120094, 120095, 120096, 120141,
        120142, 121081, 121082, 121561, 121562, 121563, 128370,
        128371, 128372, 128373,
    ];
}
