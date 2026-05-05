using System.ComponentModel;

namespace VanillaPlus.Features.BetterTeleportWindow;

public enum ListMode {

    [Description("All")]
    All,

    // Not implemented currently, but maybe planned in the future.
    [Description("History")]
    History,

    [Description("Favorites")]
    Favorites,

    [Description("Cities")]
    Cities,

    // This is a category not a specific entry, it doesn't need a description.
    Region,
}
