using Dalamud.Game.Addon.Events;
using Dalamud.Plugin.Services;

namespace VanillaPlus.Extensions;

public static class AddonEventManagerExtensions {
    extension(IAddonEventManager manager) {
        public void RemoveEventNullable(IAddonEventHandle? handle) {
            if (handle is not null) {
                manager.RemoveEvent(handle);
            }
        }
    }
}
