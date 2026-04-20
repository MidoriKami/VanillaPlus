using System;
using System.Linq;
using System.Numerics;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ListInventory;

public class ListInventory : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ListInventory,
        Description = Strings.ModificationDescription_ListInventory,
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
    };
    
    private AddonListInventory? addonListInventory;

    public override string ImageName => "ListInventory.png";

    public override void OnEnable() {
        addonListInventory = new AddonListInventory {
            InternalName = "ListInventory",
            Title = Strings.ListInventory_Title,
            Size = new Vector2(500.0f, 500.0f),
            OpenCommand = "/listinventory",
            DropDownOptions = Enum.GetValues<InventoryFilterMode>().Cast<Enum>().ToList(),
            ItemSpacing = 2.25f,
        };

        addonListInventory.Initialize();

        OpenConfigAction = addonListInventory.OpenAddonConfig;
    }

    public override void OnDisable() {
        addonListInventory?.Dispose();
        addonListInventory = null;
    }
}
