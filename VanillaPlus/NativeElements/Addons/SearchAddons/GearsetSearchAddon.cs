using System.Collections.Generic;
using System.Numerics;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using KamiToolKit.Premade.Addons;
using VanillaPlus.Features.GearsetRedirect;

namespace VanillaPlus.NativeElements.Addons.SearchAddons;

public static unsafe class GearsetSearchAddon {
    public static SearchAddon<GearsetInfo> GetAddon() => new() {
        Size = new Vector2(275.0f, 600.0f),
        InternalName = "GearsetSearch",
        Title = "Gearset Search",
        SearchOptions = [],
        SortingOptions = [ "Alphabetical", "Id" ],
    };

    public static void UpdateGearsets(this SearchAddon<GearsetInfo> addon, List<int>? omissionIds = null)
        => addon.SearchOptions = GetGearsetInfos(omissionIds ?? []);

    private static List<GearsetInfo> GetGearsetInfos(List<int> omissionIds) {
        List<GearsetInfo> entries = [];

        foreach (ref var gearset in RaptureGearsetModule.Instance()->Entries) {
            if (gearset.NameString.IsNullOrEmpty()) continue;
            if (omissionIds.Contains(gearset.Id)) continue;
        
            entries.Add(new GearsetInfo {
                GearsetId = gearset.Id,
            });
        }

        return entries;
    }
}
