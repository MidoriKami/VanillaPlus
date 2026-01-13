using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Premade.SearchAddons;
using VanillaPlus.NativeElements.SearchResultNodes;

namespace VanillaPlus.NativeElements.SearchAddons;

public unsafe class GearsetSearchAddon : BaseSearchAddon<RaptureGearsetModule.GearsetEntry, GearsetListItemNode> {
    public GearsetSearchAddon()
        => SearchOptions = [ ..RaptureGearsetModule.Instance()->Entries.ToArray() ];

    protected override int Comparer(RaptureGearsetModule.GearsetEntry left, RaptureGearsetModule.GearsetEntry right, string sortingString, bool reversed) {
        return 0;
    }

    protected override bool IsMatch(RaptureGearsetModule.GearsetEntry item, string searchString) {
        return true;
    }

    private int lastGearsetCount;
    
    protected override void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        var newCount = RaptureGearsetModule.Instance()->NumGearsets;
        if (newCount != lastGearsetCount) {
            SearchOptions = [ ..RaptureGearsetModule.Instance()->Entries.ToArray() ];
            lastGearsetCount = newCount;
        }
    }
}
