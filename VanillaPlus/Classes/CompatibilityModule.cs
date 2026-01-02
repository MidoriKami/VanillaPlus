using System;

namespace VanillaPlus.Classes;

public abstract class CompatibilityModule(string? allowedVersion = null) {
    public abstract bool ShouldLoadGameModification();

    protected bool IsPluginLoaded(string internalName) {
        foreach (var installedPlugin in Services.PluginInterface.InstalledPlugins) {
            if (installedPlugin.InternalName != internalName) continue;

            // If the installed version is less than the allowed version, return true.
            if (allowedVersion is not null) {
                return installedPlugin.Version < Version.Parse(allowedVersion);
            }

            return installedPlugin.IsLoaded;
        }

        return false;
    }

    public abstract string GetErrorMessage();
}
