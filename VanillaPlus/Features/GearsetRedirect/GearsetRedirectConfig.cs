using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.GearsetRedirect;

public class GearsetRedirectConfig : GameModificationConfig<GearsetRedirectConfig> {
    protected override string FileName => "GearsetRedirect";

    public Dictionary<int, List<RedirectInfo>> Redirections = [];
}
