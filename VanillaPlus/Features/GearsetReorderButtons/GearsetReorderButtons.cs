using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.GearsetReorderButtons;

public class GearsetReorderButtons : GameModification {

    public override ModificationInfo ModificationInfo => new() {
        // TODO: Add these to localization strings
        DisplayName = "Gearset Reorder Buttons",
        Description = "Adds buttons for easily reordering gearsets.",
        Type = ModificationType.UserInterface,
        Authors = ["zajrik"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    // public override string ImageName => "SampleGameModification.png";

    // public override bool IsExperimental => true;

    private GearSetListUiController? gearSetListUiController;

    public override void OnEnable() {
        gearSetListUiController = new();
        gearSetListUiController.OnEnable();
    }

    public override void OnDisable() {
        gearSetListUiController?.OnDisable();
        gearSetListUiController = null;
    }
}
