using VanillaPlus.Classes;

namespace VanillaPlus.Features.SuppressDialogAdvance;

public class SuppressDialogAdvanceConfig : GameModificationConfig<SuppressDialogAdvanceConfig> {
    protected override string FileName => "SuppressDialogAdvance";

    public bool ApplyOnlyInCutscenes = true;
}
