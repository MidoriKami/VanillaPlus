using VanillaPlus.Classes;

namespace VanillaPlus.Features.SampleGameModification;

// Template GameModification for more easily creating your own, can copy this entire folder and rename it.
public class SampleGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "SampleDisplayName",
        Description = "SampleDescription",
        Type = ModificationType.Hidden,
        Authors = [ "YourNameHere" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };
    
    public override void OnEnable() {
    }

    public override void OnDisable() {
    }
}
