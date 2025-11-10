using VanillaPlus.Classes;

namespace VanillaPlus.Features.SaddlebagSearchBar;

public class SaddlebagSearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Saddlebag Search Bar",
        Description = "Adds a search bar to the saddlebag window.",
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Add compatibility check"),
        ],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    private InventorySearchAddonController? saddlebagInventoryController;

    public override string ImageName => "SaddlebagSearchBar.png";

    public override void OnEnable() {
        saddlebagInventoryController = new InventorySearchAddonController("InventoryBuddy");
    }

    public override void OnDisable() {
        saddlebagInventoryController?.Dispose();
        saddlebagInventoryController = null;
    }
}
