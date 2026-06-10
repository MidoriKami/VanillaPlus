using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using VanillaPlus.Classes;
using Action = Lumina.Excel.Sheets.Action;

namespace VanillaPlus.Features.ActionHighlight.Config;

public class AntsConfig : GameModificationConfig<AntsConfig> {
    protected override string FileName => "ActionHighlightConfig";

    public override int Version => 3;

    public bool UseGlocalPreAntMs = true;
    public int PreAntTimeMs = 3000;

    public bool ShowOnlyInCombat = true;
    public bool AntOnlyOnFinalStack = true;
    public bool ShowOnlyUsableActions = true;

    public List<AntsClassJobConfig> ClassJobConfigs = [];

    protected override bool TryMigrateConfig(int? fileVersion, JObject jObject) {
        switch (fileVersion) {
            case 1:
                throw new NotSupportedException("Attempted to migrate a ActionHighlight config that was too old, only v2 -> v3 is currently supported.");

            case 2:
                var actionSettingsObj = jObject["ActionSettings"]?.ToObject<JObject>();
                if (actionSettingsObj is null) return false;

                List<AntsClassJobConfig> newEntries = [];

                foreach (var property in actionSettingsObj.Properties()) {
                    if (!uint.TryParse(property.Name, out var actionId)) continue;

                    var actionSettings = property.Value.ToObject<AntsActionSetting>();
                    if (actionSettings is null) continue;

                    var actionInfo = Services.DataManager.GetExcelSheet<Action>().GetRow(actionId);

                    var classJobEntry = newEntries.FirstOrDefault(entry => entry.ClassJobId == actionInfo.ClassJob.RowId);

                    if (classJobEntry is null) {
                        newEntries.Add(new AntsClassJobConfig {
                            ClassJobId = actionInfo.ClassJob.RowId,
                            ActionSettings = [
                                actionSettings,
                            ],
                        });
                    }
                    else {
                        classJobEntry.ActionSettings.Add(actionSettings);
                    }
                }

                ClassJobConfigs = newEntries;
                return true;
        }

        return false;
    }
}
