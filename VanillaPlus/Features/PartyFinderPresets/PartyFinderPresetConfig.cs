using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.PartyFinderPresets;

public class PartyFinderPresetConfig : GameModificationConfig<PartyFinderPresetConfig> {
    protected override string FileName => "PartyFinderPreset";

    public List<PresetEntry> Presets = [];
}
