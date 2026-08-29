using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.AdditionalHotbars.Config;

/// <summary>
/// Config file used for <see cref="AdditionalHotbars"/>
/// </summary>
public class AdditionalHotbarsConfig : GameModificationConfig<AdditionalHotbarsConfig> {
    protected override string FileName => "AdditionalHotbarsConfig";

    public List<HotbarConfig> Hotbars = [];
}
