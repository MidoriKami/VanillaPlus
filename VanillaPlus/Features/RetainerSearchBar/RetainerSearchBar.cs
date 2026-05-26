using System.Threading.Tasks;
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

    public override async Task OnEnableAsync() {
        retainerInventoryController = new InventorySearchAddonController("InventoryRetainerLarge", "InventoryRetainer");

        await retainerInventoryController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        if (retainerInventoryController is not null) {
            await retainerInventoryController.DisposeAsync();
            retainerInventoryController = null;
        }
    }
}
