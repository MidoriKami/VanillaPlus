using VanillaPlus.Classes;
using VanillaPlus.InternalSystem;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus;

public static class PluginSystem {
    public static SystemConfiguration SystemConfig { get; set; } = null!;
    public static AddonModificationBrowser AddonModificationBrowser { get; set; } = null!;
    public static SeasonEventAddon SeasonEventAddon { get; set; } = null!;
    public static ModificationManager ModificationManager { get; set; } = null!;
    public static KeyListener KeyListener { get; set; } = null!;
}
