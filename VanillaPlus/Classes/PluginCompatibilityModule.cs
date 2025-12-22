namespace VanillaPlus.Classes;

public class PluginCompatibilityModule(params string[] pluginNames) : CompatibilityModule {

    private string erroringPluginName = string.Empty;
    
    public override bool ShouldLoadGameModification() {
        foreach (var pluginName in pluginNames) {
            if (IsPluginLoaded(pluginName)) {
                erroringPluginName = pluginName;
                return false;
            }
        }

        return true;
    }

    public override string GetErrorMessage()
        => Strings("CompatibilityModule_ActivePluginMessage", erroringPluginName);
}
