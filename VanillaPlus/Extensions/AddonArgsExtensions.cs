using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;

namespace VanillaPlus.Extensions;

public static unsafe class AddonArgsExtensions {
    public static T* GetAddon<T>(this AddonArgs args) where T : unmanaged
        => (T*)args.Addon.Address;
}
