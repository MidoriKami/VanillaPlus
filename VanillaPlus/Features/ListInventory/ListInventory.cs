using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Keys;
using KamiToolKit;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Addons;
using VanillaPlus.Utilities;

namespace VanillaPlus.Features.ListInventory;

public class ListInventory : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Inventory List Window",
        Description = "Adds a window that displays your inventory as a list, with toggleable filters.",
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Added Sort by Quantity"),
            new ChangeLogInfo(3, "Added '/listinventory' command to open window"),
            new ChangeLogInfo(4, "Sort Dropdown is now on another line, added reverse sort direction button"),
            new ChangeLogInfo(5, "Renamed to be consistent with other features"),
        ],
    };
    
    private SearchableNodeListAddon? addonListInventory;

    private string filterString = string.Empty;
    private string searchString = string.Empty;
    private bool filterReversed;
    private bool updateRequested;

    public override string ImageName => "ListInventory.png";

    public override void OnEnable() {
        addonListInventory = new SearchableNodeListAddon {
            InternalName = "ListInventory",
            Title = "Inventory List",
            Size = new Vector2(450.0f, 700.0f),
            OnFilterUpdated = OnFilterUpdated,
            OnSearchUpdated = OnSearchUpdated,
            UpdateListFunction = OnListUpdated,
            DropDownOptions = ["Alphabetically", "Quantity", "Level", "Item Level", "Rarity", "Item Id", "Item Category"],
            OpenCommand = "/listinventory",
        };

        addonListInventory.Initialize(new Keybind {
            Key = VirtualKey.I,
            Modifiers = [ VirtualKey.CONTROL, VirtualKey.MENU ],
        });

        OpenConfigAction = addonListInventory.OpenAddonConfig;

        Services.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "Inventory", OnInventoryUpdate);
        
        updateRequested = true;
    }

    public override void OnDisable() {
        addonListInventory?.Dispose();
        addonListInventory = null;
        
        Services.AddonLifecycle.UnregisterListener(OnInventoryUpdate);
    }
    
    private void OnInventoryUpdate(AddonEvent type, AddonArgs args) {
        updateRequested = true;
        addonListInventory?.DoListUpdate();
    }

    private void OnFilterUpdated(string newFilterString, bool reversed) {
        updateRequested = true;
        filterString = newFilterString;
        filterReversed = reversed;
        addonListInventory?.DoListUpdate();
    }

    private void OnSearchUpdated(string newSearchString) {
        updateRequested = true;
        searchString = newSearchString;
        addonListInventory?.DoListUpdate();
    }

    private bool OnListUpdated(VerticalListNode list, bool isOpening) {
        if (!updateRequested && !isOpening) return false;

        var filteredInventoryItems = Inventory.GetInventoryItems(searchString);

        var listUpdated = list.SyncWithListData(filteredInventoryItems, node => node.Item, data => new InventoryItemNode {
            Size = new Vector2(list.Width, 32.0f),
            Item = data,
        });

        list.ReorderNodes(Comparison);
        
        updateRequested = false;
        return listUpdated;
    }
    
    private int Comparison(NodeBase x, NodeBase y) {
        if (x is not InventoryItemNode left || y is not InventoryItemNode right) return 0;

        var leftItem = left.Item;
        var rightItem = right.Item;
        if (leftItem is null || rightItem is null) return 0;

        // Note: Compares in opposite direction to be descending instead of ascending, except for alphabetically

        var result = filterString switch {
            "Alphabetically" => string.CompareOrdinal(leftItem.Name, rightItem.Name),
            "Level" => rightItem.Level.CompareTo(leftItem.Level),
            "Item Level" => rightItem.ItemLevel.CompareTo(leftItem.ItemLevel),
            "Rarity" => rightItem.Rarity.CompareTo(leftItem.Rarity),
            "Item Id" => rightItem.Item.ItemId.CompareTo(leftItem.Item.ItemId),
            "Item Category" => rightItem.UiCategory.CompareTo(leftItem.UiCategory),
            "Quantity" => rightItem.ItemCount.CompareTo(leftItem.ItemCount),
            _ => string.CompareOrdinal(leftItem.Name, rightItem.Name),
        };

        var reverseModifier = filterReversed ? -1 : 1;
        
        return ( result is 0 ? string.CompareOrdinal(leftItem.Name, rightItem.Name) : result ) * reverseModifier;
    }
}
