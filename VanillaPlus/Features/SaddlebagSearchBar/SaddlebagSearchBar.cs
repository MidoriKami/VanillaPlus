using System.Threading.Tasks;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.SaddlebagSearchBar;

public class SaddlebagSearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_SaddlebagSearchBar,
        Description = Strings.ModificationDescription_SaddlebagSearchBar,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    private InventorySearchAddonController? saddlebagInventoryController;

    public override string ImageName => "SaddlebagSearchBar.png";

    public override Task OnEnableAsync() {
        saddlebagInventoryController = new InventorySearchAddonController("InventoryBuddy");

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        saddlebagInventoryController?.Dispose();
        saddlebagInventoryController = null;

        return Task.CompletedTask;
    }
}
