using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.InstancedWaymarks;

public class InstancedWaymarksConfig : GameModificationConfig<InstancedWaymarksConfig> {
    protected override string FileName => "InstancedWaymarks";

    public Dictionary<uint, Dictionary<int, string>> NamedWaymarks = [];
}
