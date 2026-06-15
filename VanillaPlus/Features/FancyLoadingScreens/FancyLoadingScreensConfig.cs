using VanillaPlus.Classes;

namespace VanillaPlus.Features.FancyLoadingScreens;

public class FancyLoadingScreensConfig : GameModificationConfig<FancyLoadingScreensConfig> {
    protected override string FileName => "FancyLoadingScreens";

    // Show the destination art on instanced loads (housing, duties, ...) that otherwise stay black.
    public bool ShowOnInstancedLoad;
}
