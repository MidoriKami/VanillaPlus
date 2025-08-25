using System.Linq;

namespace VanillaPlus.Classes;

public abstract class CompatibilityModule {
    public abstract bool ShouldLoadGameModification();

    protected static bool IsPluginLoaded(string internalName)
        => Services.PluginInterface.InstalledPlugins.FirstOrDefault(installed => installed.InternalName == internalName)?.IsLoaded ?? false;

    public abstract string GetErrorMessage();
}
