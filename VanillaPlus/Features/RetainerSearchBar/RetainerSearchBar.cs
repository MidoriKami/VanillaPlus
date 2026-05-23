using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.RetainerSearchBar;

public class RetainerSearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_RetainerSearchBar,
        Description = Strings.ModificationDescription_RetainerSearchBar,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    private InventorySearchAddonController? retainerInventoryController;

    public override string ImageName => "RetainerSearchBar.png";

    public override void OnEnableAsync() {
        retainerInventoryController = new InventorySearchAddonController("InventoryRetainerLarge", "InventoryRetainer");
    }

    public override void OnDisableAsync() {
        retainerInventoryController?.Dispose();
        retainerInventoryController = null;
    }
}
