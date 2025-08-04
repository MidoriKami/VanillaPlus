using VanillaPlus.Classes;

namespace VanillaPlus.LocationDisplay;

public class LocationDisplayConfig : GameModificationConfig<LocationDisplayConfig> {
    protected override string FileName => "LocationDisplay.config.json";

    public string FormatString = "{0}, {1}, {2}, {3}";
    public bool ShowInstanceNumber = true;
    public string TooltipFormatString = "{0}, {1}, {2}, {3}";
    public bool UsePreciseHousingLocation = false;
}
