using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ActionHighlight;

public class ActionHighlightConfig : GameModificationConfig<ActionHighlightConfig> {
    protected override string FileName => "ActionHighlightConfig";

    public override int Version => 2;

    public bool UseGlocalPreAntMs = true;
    public int PreAntTimeMs = 3000;

    public bool ShowOnlyInCombat = true;
    public bool AntOnlyOnFinalStack = true;
    public bool ShowOnlyUsableActions = true;

    public Dictionary<uint, ActionHighlightSetting> ActionSettings { get; set; } = [];

    protected override bool TryMigrateConfig(int? fileVersion, JObject jObject) {
        switch (fileVersion) {
            case 1: 
                var oldActions = jObject["ActiveActions"]?.ToObject<Dictionary<uint, int>>();
                if (oldActions is null) return true;

                ActionSettings.Clear();
                foreach (var (id, ms) in oldActions) {
                    ActionSettings[id] = new ActionHighlightSetting {
                        ActionId = id,
                        ThresholdMs = ms,
                        IsEnabled = true,
                    };
                }
                return true;
        }

        return false;
    }
}
