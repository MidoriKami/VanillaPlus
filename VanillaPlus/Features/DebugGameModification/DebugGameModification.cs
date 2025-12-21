using VanillaPlus.Classes;

namespace VanillaPlus.Features.DebugGameModification;

#if DEBUG
/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings("ModificationDisplay_DebugGameModification"),
        Description = Strings("ModificationDescription_DebugGameModification"),
        Type = ModificationType.Debug,
        Authors = [ "YourNameHere" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override void OnEnable() {
    }

    public override void OnDisable() {
    }
}
#endif
