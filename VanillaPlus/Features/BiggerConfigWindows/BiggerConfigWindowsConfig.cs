using VanillaPlus.Classes;

namespace VanillaPlus.Features.BiggerConfigWindows;

public class BiggerConfigWindowsConfig : GameModificationConfig<BiggerConfigWindowsConfig> {

    protected override string FileName => "BiggerConfigWindows";

    public float SystemConfigAdditionalHeight = 350.0f;
    public float CharacterConfigAdditionalHeight = 350.0f;
}
