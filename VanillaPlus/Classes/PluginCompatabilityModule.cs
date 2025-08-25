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
        => $"The original version of this feature is from a plugin that is currently active: {erroringPluginName}.";
}
