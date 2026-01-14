using System;
using System.Linq;
using System.Numerics;
using VanillaPlus.Classes;
using VanillaPlus.Utilities;

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
    
    private AddonListInventory? addonListInventory;

    public override string ImageName => "ListInventory.png";

    public override void OnEnable() {
        addonListInventory = new AddonListInventory {
            InternalName = "ListInventory",
            Title = Strings.ListInventory_Title,
            Size = new Vector2(500.0f, 500.0f),
            OpenCommand = "/listinventory",
            DropDownOptions = Enum.GetValues<InventoryFilterMode>().Select(value => value.Description).ToList(),
            ItemSpacing = 2.25f,
            ListItems = Inventory.GetInventoryStacks().ToList(),
        };

        addonListInventory.Initialize();

        OpenConfigAction = addonListInventory.OpenAddonConfig;
    }

    public override void OnDisable() {
        addonListInventory?.Dispose();
        addonListInventory = null;
    }
}
