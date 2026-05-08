using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.BetterTeleportWindow;

public class BetterTeleportWindowConfig : GameModificationConfig<BetterTeleportWindowConfig> {
    protected override string FileName => "BetterTeleportWindow";

    public List<uint> FavoriteAetherytes = [];
    public Dictionary<uint, string> CustomNames = [];
    public bool AutoFocusSearch = true;
    public ListMode LastListMode = ListMode.All;
}
