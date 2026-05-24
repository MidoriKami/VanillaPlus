using System.Threading.Tasks;
using Dalamud.Game.Config;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ArmourySearchBar;

public class ArmourySearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ArmourySearchBar,
        Description = Strings.ModificationDescription_ArmourySearchBar,
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = ["MidoriKami"],
        CompatibilityModule = new PluginCompatibilityModule("InventorySearchBar"),
    };

    private InventorySearchAddonController? armouryInventoryController;

    private bool? configFadeUnusable;
    private bool searchStarted;

    public override string ImageName => "ArmourySearchBar.png";

    public override Task OnEnableAsync() {
        armouryInventoryController = new InventorySearchAddonController("ArmouryBoard");

        armouryInventoryController.PreSearch += searchString => {
            if (configFadeUnusable is null) {
                Services.GameConfig.TryGet(UiConfigOption.ItemNoArmoryMaskOff, out bool value);
                configFadeUnusable = value;
            }

            if (!searchString.ToString().IsNullOrEmpty() && !searchStarted) {
                Services.GameConfig.Set(UiConfigOption.ItemNoArmoryMaskOff, true);
                searchStarted = true;
            }

            if (searchStarted && searchString.ToString().IsNullOrEmpty()) {
                Services.GameConfig.Set(UiConfigOption.ItemNoArmoryMaskOff, configFadeUnusable.Value);
                searchStarted = false;
            }
        };

        return Task.CompletedTask;
    }

    public override Task OnDisableAsync() {
        armouryInventoryController?.Dispose();
        armouryInventoryController = null;

        return Task.CompletedTask;
    }
}
