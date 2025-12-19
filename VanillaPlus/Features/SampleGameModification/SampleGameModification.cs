using VanillaPlus.Classes;

namespace VanillaPlus.Features.SampleGameModification;

// Template GameModification for more easily creating your own, can copy this entire folder and rename it.
public class SampleGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_SampleGameModification"),
        Description = Strings("ModificationDescription_SampleGameModification"),
        Type = ModificationType.Hidden,
        Authors = [ "YourNameHere" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    // public override string ImageName => "SampleGameModification.png";

    // public override bool IsExperimental => true;

    public override void OnEnable() {
    }

    public override void OnDisable() {
    }
}
