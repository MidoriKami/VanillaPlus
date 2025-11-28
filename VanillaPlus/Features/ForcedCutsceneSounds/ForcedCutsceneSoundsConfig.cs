using VanillaPlus.Classes;

namespace VanillaPlus.Features.ForcedCutsceneSounds;

public class ForcedCutsceneSoundsConfig : GameModificationConfig<ForcedCutsceneSoundsConfig> {
    protected override string FileName => "ForcedCutsceneSounds";
    
    public bool Restore = true;
    public bool HandleMaster = true;
    public bool HandleBgm = true;
    public bool HandleSe = true;
    public bool HandleVoice = true;
    public bool HandleEnv = true;
    public bool HandleSystem = false;
    public bool HandlePerform = false;

    public bool DisableInMsqRoulette = true;
}
