using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;
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

                var classJobs = Services.DataManager.GetExcelSheet<ClassJob>()
                    .Where(job => job is { RowId: not 0, Name.IsEmpty: false, IsCrafter: false, IsGatherer: false })
                    .ToList();

                List<AntsClassJobConfig> newEntries = [];

                foreach (var property in actionSettingsObj.Properties()) {
                    if (!uint.TryParse(property.Name, out var actionId)) continue;

                    var actionSettings = property.Value.ToObject<AntsActionSetting>();
                    if (actionSettings is null) continue;

                    var action = Services.DataManager.GetExcelSheet<Action>().GetRow(actionId);

                    foreach (var classJob in classJobs) {
                        if (!ActionHighlight.IsValidAction(action, classJob)) continue;
                        if (!ActionHighlight.IsValidRoleAction(action, classJob)) continue;

                        AddActionSetting(newEntries, classJob.RowId, actionSettings, actionId);
                    }
                }

                ClassJobConfigs = newEntries;
                return true;
        }

        return false;
    }

    private static void AddActionSetting(List<AntsClassJobConfig> entries, uint classJobId, AntsActionSetting source, uint actionId) {
        if (entries.FirstOrDefault(configEntry => configEntry.ClassJobId == classJobId) is not { } entry) {
            entry = new AntsClassJobConfig {
                ClassJobId = classJobId,
                ActionSettings = [],
            };

            entries.Add(entry);
        }

        if (entry.ActionSettings.Any(setting => setting.ActionId == actionId)) return;

        entry.ActionSettings.Add(new AntsActionSetting {
            ActionId = actionId,
            IsEnabled = source.IsEnabled,
            ThresholdMs = source.ThresholdMs,
        });
    }
}
