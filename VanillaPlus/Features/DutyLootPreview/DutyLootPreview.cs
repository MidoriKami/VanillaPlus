using System.Numerics;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DutyLootPreview;

public class DutyLootPreview : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Duty Loot Preview",
        Description = "Adds a duty loot viewer to the duty window.",
        Type = ModificationType.NewWindow,
        Authors = [ "GrittyFrog" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Added Context Menu, Filter Buttons, Favorites and Item Drop info")
        ],
    };

    public override string ImageName => "DutyLootPreview.png";

    private DutyLootUiController? contentFinderController;
    private DutyLootPreviewAddon? addonDutyLoot;
    private DutyLootPreviewConfig? config;

    public override void OnEnable() {
        config = DutyLootPreviewConfig.Load();
        
        addonDutyLoot = new DutyLootPreviewAddon {
            InternalName = "DutyLootPreview",
            Title = "Duty Loot Preview",
            Size = new Vector2(300.0f, 350.0f),
            Config = config,
        };

        contentFinderController = new DutyLootUiController {
            OnButtonClicked = addonDutyLoot.Toggle,
        };
        contentFinderController.OnEnable();
    }

    public override void OnDisable() {
        contentFinderController?.OnDisable();
        contentFinderController = null;

        addonDutyLoot?.Dispose();
        addonDutyLoot = null;

        config = null;
    }
}
