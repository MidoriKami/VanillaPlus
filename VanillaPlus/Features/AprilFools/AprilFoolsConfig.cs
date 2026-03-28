using VanillaPlus.Classes;

namespace VanillaPlus.Features.AprilFools;

public class AprilFoolsConfig : GameModificationConfig<AprilFoolsConfig> {
    protected override string FileName => "AprilFools";

    public bool InvertScroll = true;
    public bool InsaneScrollMode = false;
    public bool Indecisive = true;
    public bool EmotionalDamage = true;
    public bool JustMonika = true;
    public bool DutyPop = true;
    public bool BetterCharacterPanel = true;
    public bool FlippingOut = true;
}
