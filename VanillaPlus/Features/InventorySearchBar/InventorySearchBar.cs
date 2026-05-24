using System.Threading.Tasks;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.InventorySearchBar;

public class InventorySearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_InventorySearchBar,
        Description = Strings.ModificationDescription_InventorySearchBar,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    public override string ImageName => "InventorySearchBar.png";

    private InventorySearchAddonController? inventoryController;

    public override Task OnEnableAsync() {
        inventoryController = new InventorySearchAddonController("InventoryExpansion", "InventoryLarge", "Inventory");

        return Task.CompletedTask;
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            inventoryController?.Dispose();
            inventoryController = null;
        });
    }
}
