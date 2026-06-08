using VanillaPlus.Classes;
using VanillaPlus.Native.Addons;

namespace VanillaPlus;

public static class System {
    public static SystemConfiguration SystemConfig { get; set; } = null!;
    public static ModificationBrowserAddon ModificationBrowserAddon { get; set; } = null!;
    public static SeasonEventAddon SeasonEventAddon { get; set; } = null!;
    public static ModificationManager ModificationManager { get; set; } = null!;
    public static KeyListener KeyListener { get; set; } = null!;
}
