using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.CurrencyOverlay;

public class CurrencyOverlayConfig : GameModificationConfig<CurrencyOverlayConfig> {
    protected override string FileName => "CurrencyOverlay.config.json";

    public List<CurrencySettings> Currencies = [];
}
