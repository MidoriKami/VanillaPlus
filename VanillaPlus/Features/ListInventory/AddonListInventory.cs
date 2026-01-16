using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Addons;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.ListInventory;

public class AddonListInventory : SearchableNodeListAddon<ItemStack, InventoryItemNode> {
    private InventoryFilterMode lastSortingMode = InventoryFilterMode.Alphabetical;
    private bool isReversed;
    private string lastSearchString = string.Empty;
    
    public AddonListInventory() {
        OnSortingUpdated = UpdateSorting;
        OnSearchUpdated = UpdateSearch;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        base.OnSetup(addon);

        addon->SubscribeNumberArrayData(NumberArrayType.Inventory);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "Inventory", OnInventoryUpdate);
    }

    protected override unsafe void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData) {
        UpdateInventoryItems();
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        base.OnFinalize(addon);
        
        addon->UnsubscribeNumberArrayData(NumberArrayType.Inventory);
        Services.AddonLifecycle.UnregisterListener(OnInventoryUpdate);
    }

    private void OnInventoryUpdate(AddonEvent type, AddonArgs args) {
        UpdateInventoryItems();
    }

    private void UpdateInventoryItems() {
        Services.PluginLog.Debug("Inventory Updated");
        ListItems = Inventory.GetInventoryStacks().Where(item => ItemStack.IsMatch(item, lastSearchString)).ToList();
        ListItems.Sort((left, right) => ItemStack.Comparison(left, right, lastSortingMode) * (isReversed ? -1 : 1));
    }
    
    private void UpdateSorting(string newFilterString, bool reversed) {
        var enumValue = newFilterString.ParseAsEnum(InventoryFilterMode.Alphabetical);

        lastSortingMode = enumValue;
        isReversed = reversed;
        
        UpdateInventoryItems();
    }
    
    private void UpdateSearch(string searchString) {
        lastSearchString = searchString;
        
        UpdateInventoryItems();
    }
}
