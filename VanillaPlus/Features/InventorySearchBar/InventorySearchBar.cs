using VanillaPlus.Classes;

namespace VanillaPlus.Features.InventorySearchBar;

public class InventorySearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Inventory Search Bar",
        Description = "Adds a search bar to the inventory window.",
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Add compatibility check"),
        ],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    public override string ImageName => "InventorySearchBar.png";

    private InventorySearchAddonController? inventoryController;

    public override void OnEnable() {
        inventoryController = new InventorySearchAddonController("InventoryExpansion", "InventoryLarge", "Inventory");
    }

    public override void OnDisable() {
        inventoryController?.Dispose();
        inventoryController = null;
    }
}
