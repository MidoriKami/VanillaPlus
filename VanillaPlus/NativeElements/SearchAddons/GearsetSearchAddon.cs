using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Premade.SearchAddons;
using VanillaPlus.NativeElements.ListItemNodes;

namespace VanillaPlus.NativeElements.SearchAddons;

public unsafe class GearsetSearchAddon : BaseSearchAddon<RaptureGearsetModule.GearsetEntry, GearsetListItemNode> {
    protected override int Comparer(RaptureGearsetModule.GearsetEntry left, RaptureGearsetModule.GearsetEntry right, string sortingString, bool reversed) {
        return sortingString switch {
            "Alphabetical" => string.Compare(left.NameString, right.NameString, StringComparison.Ordinal),
            "Id" => left.Id.CompareTo(right.Id),
            _ => 0,
        } * (reversed ? -1 : 1);
    }

    protected override bool IsMatch(RaptureGearsetModule.GearsetEntry item, string searchString) {
        var regex = new Regex(searchString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return regex.IsMatch(item.NameString);
    }

    private int lastGearsetCount;
    
    protected override void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        var newCount = RaptureGearsetModule.Instance()->NumGearsets;
        if (newCount != lastGearsetCount) {
            SearchOptions = GetGearsetEntries();
            lastGearsetCount = newCount;
        }
    }

    private List<RaptureGearsetModule.GearsetEntry> GetGearsetEntries() {
        List<RaptureGearsetModule.GearsetEntry> entries = [];
        
        entries.AddRange(Enumerable.Range(0, RaptureGearsetModule.Instance()->NumGearsets)
            .Select(index => RaptureGearsetModule.Instance()->Entries[index]));

        return entries;
    }
}
