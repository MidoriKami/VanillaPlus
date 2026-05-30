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

    public override async Task OnEnableAsync() {
        armouryInventoryController = new InventorySearchAddonController("ArmouryBoard");
        armouryInventoryController.PreSearch += OnPreSearch;

        await Services.Framework.Run(armouryInventoryController.Enable);
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => armouryInventoryController?.Dispose());
        armouryInventoryController = null;
    }

    private void OnPreSearch(string searchString) {
        if (configFadeUnusable is null) {
            Services.GameConfig.TryGet(UiConfigOption.ItemNoArmoryMaskOff, out bool value);
            configFadeUnusable = value;
        }

        if (!searchString.IsNullOrEmpty() && !searchStarted) {
            Services.GameConfig.Set(UiConfigOption.ItemNoArmoryMaskOff, true);
            searchStarted = true;
        }

        if (searchStarted && searchString.IsNullOrEmpty()) {
            Services.GameConfig.Set(UiConfigOption.ItemNoArmoryMaskOff, configFadeUnusable.Value);
            searchStarted = false;
        }
    }

}
