using System.ComponentModel;

namespace VanillaPlus.Features.BetterTeleportWindow;

public enum ListMode {

    [Description("BetterTeleportWindow_ModeAll")]
    All,

    // Not implemented currently, but maybe planned in the future.
    [Description("BetterTeleportWindow_ModeHistory")]
    History,

    [Description("BetterTeleportWindow_ModeFavorites")]
    Favorites,

    [Description("BetterTeleportWindow_ModeCities")]
    Cities,

    // This is a category not a specific entry, it doesn't need a description.
    Region,
}
