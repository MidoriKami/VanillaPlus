using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.AdditionalHotbarSlots.Config;

public class AdditionalHotbarsConfig : GameModificationConfig<AdditionalHotbarsConfig> {
    protected override string FileName => "AdditionalHotbarsConfig";

    public List<HotbarConfig> Hotbars = [];
}
