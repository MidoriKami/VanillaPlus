﻿using Dalamud.Game.Config;
using Dalamud.Utility;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.ArmourySearchBar;

public class ArmourySearchBar : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Armoury Search Bar",
        Description = "Adds a search bar to the armoury window.",
        Type = ModificationType.UserInterface,
        SubType = ModificationSubType.Inventory,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private InventorySearchAddonController? armouryInventoryController;

    private bool? configFadeUnusable;
    private bool searchStarted;

    public override string ImageName => "ArmourySearchBar.png";

    public override void OnEnable() {
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
    }

    public override void OnDisable() {
        armouryInventoryController?.Dispose();
        armouryInventoryController = null;
    }
}
