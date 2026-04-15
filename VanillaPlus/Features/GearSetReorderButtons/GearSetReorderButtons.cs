using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.GearSetReorderButtons;

public class GearSetReorderButtons : GameModification {

    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.GearSetReorderButtons_DisplayName,
        Description = Strings.GearSetReorderButtons_Description,
        Type = ModificationType.UserInterface,
        Authors = ["zajrik"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "GearSetReorderButtons.png";

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
