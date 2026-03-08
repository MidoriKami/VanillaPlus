using Dalamud.Game.Agent;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.DebugGameModification;

#if DEBUG
/// <summary>
/// Debug Game Modification for use with playing around with ideas, DO NOT commit changes to this file
/// </summary>
public class DebugGameModification : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_DebugGameModification,
        Description = Strings.ModificationDescription_DebugGameModification,
        Type = ModificationType.Debug,
        Authors = [ "YourNameHere" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override void OnEnable() {
        Services.AgentLifecycle.LogAgent(AgentId.Config);
    }

    public override void OnDisable() {
        Services.AgentLifecycle.UnLogAgent(AgentId.Config);
    }
}
#endif
