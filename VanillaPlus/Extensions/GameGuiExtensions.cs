using Dalamud.Plugin.Services;

namespace VanillaPlus.Extensions;

public static class GameGuiExtensions {
    public static unsafe T* GetAddonByName<T>(this IGameGui gameGui, string name) where T : unmanaged
        => (T*) gameGui.GetAddonByName(name).Address;
}
