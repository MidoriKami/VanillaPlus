using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.PartyFinderPresets;

public class PartyFinderPresets : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_PartyFinderPresets,
        Description = Strings.ModificationDescription_PartyFinderPresets,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "PartyFinderPresets.png";

    private MainWindowController? mainWindowController;
    private RecruitmentWindowController? recruitmentWindowController;

    private PartyFinderPresetConfig? config;

    public override void OnEnable() {
        config = PartyFinderPresetConfig.Load();

        mainWindowController = new MainWindowController(config);
        recruitmentWindowController = new RecruitmentWindowController(config);
    }

    public override void OnDisable() {
        mainWindowController?.Dispose();
        mainWindowController = null;

        recruitmentWindowController?.Dispose();
        recruitmentWindowController = null;

        config = null;
    }
}
