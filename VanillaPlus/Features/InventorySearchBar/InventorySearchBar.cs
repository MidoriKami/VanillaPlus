using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.InventorySearchBar;

public class InventorySearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_InventorySearchBar,
        Description = Strings.ModificationDescription_InventorySearchBar,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = [ "MidoriKami" ],
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
