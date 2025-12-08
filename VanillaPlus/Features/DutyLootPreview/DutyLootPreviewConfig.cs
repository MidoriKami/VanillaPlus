using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DutyLootPreview;

public class DutyLootPreviewConfig : GameModificationConfig<DutyLootPreviewConfig> {
    protected override string FileName => "DutyLootPreview";

    public HashSet<uint> FavoriteItems = [];
}
