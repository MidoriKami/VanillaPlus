using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ActionHighlight;

public class ActionHighlightConfig : GameModificationConfig<ActionHighlightConfig> {
    protected override string FileName => "ActionHighlightConfig";

    public bool UseGlocalPreAntMs = true;
    public int PreAntTimeMs = 3000;

    public bool ShowOnlyInCombat = true;
    public bool AntOnlyOnFinalStack = true;
    public bool ShowOnlyUsableActions = true;

    public Dictionary<uint, int> ActiveActions = [];
}
