using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.PartyFinderPresets;

public class PartyFinderPresets : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_PartyFinderPresets,
        Description = Strings.ModificationDescription_PartyFinderPresets,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
        DisabledReason = "Currently Unavailable\n\n" +
                         "Temporarily disabled this feature due to bugs.\n" +
                         "Fix is planned, but will take time. Sorry for the trouble.",
    };

    public override string ImageName => "PartyFinderPresets.png";

    private MainWindowController? mainWindowController;
    private RecruitmentWindowController? recruitmentWindowController;

    private PartyFinderPresetConfig? config;

    public override void OnEnableAsync() {
        config = PartyFinderPresetConfig.Load();

        mainWindowController = new MainWindowController(config);
        recruitmentWindowController = new RecruitmentWindowController(config);
    }

    public override void OnDisableAsync() {
        mainWindowController?.Dispose();
        mainWindowController = null;

        recruitmentWindowController?.Dispose();
        recruitmentWindowController = null;

        config = null;
    }
}
