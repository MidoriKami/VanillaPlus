using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.GearsetRedirect;

public class GearsetRedirectConfig : GameModificationConfig<GearsetRedirectConfig> {
    protected override string FileName => "GearsetRedirect";
    public override int Version => 2;

    public List<GearsetRedirectionEntry> GearsetEntries = [];

    protected override bool TryMigrateConfig(int? fileVersion, JObject jObject) {
        switch (fileVersion) {
            case 1:
                GearsetEntries = jObject["Redirections"]?
                    .ToObject<Dictionary<int, List<RedirectionConfig>>>()?
                    .Select(ParseOldRedirectionEntry)
                    .ToList() ?? [];

                return true;
        }

        return false;
    }

    private static GearsetRedirectionEntry ParseOldRedirectionEntry(KeyValuePair<int, List<RedirectionConfig>> kvp) => new() {
        TargetGearsetId = kvp.Key,
        Redirections = kvp.Value,
    };
}
