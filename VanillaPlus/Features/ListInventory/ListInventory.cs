using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using KamiToolKit.Premade.SearchResultNodes;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.ListInventory;

public class ListInventory : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ListInventory,
        Description = Strings.ModificationDescription_ListInventory,
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Added Sort by Quantity"),
            new ChangeLogInfo(3, "Added '/listinventory' command to open window"),
            new ChangeLogInfo(4, "Sort Dropdown is now on another line, added reverse sort direction button"),
            new ChangeLogInfo(5, "Renamed to be consistent with other features"),
            new ChangeLogInfo(6, "Using consumables or moving items now updates the list"),
        ],
    };
    
    private SearchableNodeListAddon<Item, ItemListItemNode>? addonListInventory;

    public override string ImageName => "ListInventory.png";

    public override void OnEnable() {
        addonListInventory = new SearchableNodeListAddon<Item, ItemListItemNode> {
            InternalName = "ListInventory",
            Title = Strings.ListInventory_Title,
            Size = new Vector2(450.0f, 700.0f),
            OnSortingUpdated = OnFilterUpdated,
            OnSearchUpdated = OnSearchUpdated,
            DropDownOptions = Enum.GetValues<InventoryFilterMode>().Select(value => value.Description).ToList(),
            OpenCommand = "/listinventory",
            ListItems = [],
        };

        addonListInventory.Initialize();

        OpenConfigAction = addonListInventory.OpenAddonConfig;

        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "Inventory", OnInventoryUpdate);
    }

    public override void OnDisable() {
        addonListInventory?.Dispose();
        addonListInventory = null;
        
        Services.AddonLifecycle.UnregisterListener(OnInventoryUpdate);
    }
    
    private void OnInventoryUpdate(AddonEvent type, AddonArgs args) {
    }

    // private void OnInventoryChanged() {
    // }

    private void OnFilterUpdated(string newFilterString, bool reversed) {
    }

    private void OnSearchUpdated(string newSearchString) {
    }

    //
    // private int Comparison(NodeBase x, NodeBase y) {
    //     if (x is not InventoryItemNode left || y is not InventoryItemNode right) return 0;
    //
    //     var leftItem = left.Item;
    //     var rightItem = right.Item;
    //     if (leftItem is null || rightItem is null) return 0;
    //
    //     // Note: Compares in opposite direction to be descending instead of ascending, except for alphabetically
    //
    //     var result = filterString switch {
    //         var s when s == FilterAlphabeticallyLabel  => string.CompareOrdinal(leftItem.Name, rightItem.Name),
    //         var s when s == FilterLevelLabel => rightItem.Level.CompareTo(leftItem.Level),
    //         var s when s == FilterItemLevelLabel  => rightItem.ItemLevel.CompareTo(leftItem.ItemLevel),
    //         var s when s == FilterRarityLabel  => rightItem.Rarity.CompareTo(leftItem.Rarity),
    //         var s when s == FilterItemIdLabel => rightItem.Item.ItemId.CompareTo(leftItem.Item.ItemId),
    //         var s when s == FilterItemCategoryLabel => rightItem.UiCategory.CompareTo(leftItem.UiCategory),
    //         var s when s == FilterQuantityLabel => rightItem.ItemCount.CompareTo(leftItem.ItemCount),
    //         _ => string.CompareOrdinal(leftItem.Name, rightItem.Name),
    //     };
    //
    //     var reverseModifier = filterReversed ? -1 : 1;
    //     
    //     return ( result is 0 ? string.CompareOrdinal(leftItem.Name, rightItem.Name) : result ) * reverseModifier;
    // }
}
