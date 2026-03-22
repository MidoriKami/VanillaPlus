namespace VanillaPlus.Classes;

public class AprilFoolsCompatabilityModule : CompatibilityModule {

    public override bool ShouldLoadGameModification() {
        return Services.PluginInterface.AllowSeasonalEvents;
    }

    public override string GetErrorMessage()
        => "Seasonal events are disabled in Dalamud settings use '/xlsettings' to change this setting.";
}
