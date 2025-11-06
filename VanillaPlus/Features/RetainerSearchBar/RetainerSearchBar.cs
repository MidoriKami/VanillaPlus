using VanillaPlus.Classes;

namespace VanillaPlus.Features.RetainerSearchBar;

public class RetainerSearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Retainer Search Bar",
        Description = "Adds a search bar to the retainer window.",
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
            new ChangeLogInfo(2, "Add compatibility check"),
        ],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    private InventorySearchAddonController? retainerInventoryController;

    public override string ImageName => "RetainerSearchBar.png";

    public override void OnEnable() {
        retainerInventoryController = new InventorySearchAddonController("InventoryRetainerLarge", "InventoryRetainer");
    }

    public override void OnDisable() {
        retainerInventoryController?.Dispose();
        retainerInventoryController = null;
    }
}
