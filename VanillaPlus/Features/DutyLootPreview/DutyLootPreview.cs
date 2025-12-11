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
            new ChangeLogInfo(2, "Added Context Menu, Filter Buttons, Favorites and Item Drop info"),
            new ChangeLogInfo(3, "Added button to view current duty loot")
        ],
    };

    public override string ImageName => "DutyLootPreview.png";

    private DutyLootJournalUiController? journalUiController;
    private DutyLootInDutyUiController? inDutyUiController;
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

        journalUiController = new DutyLootJournalUiController {
            OnButtonClicked = addonDutyLoot.Toggle,
        };
        journalUiController.OnEnable();

        inDutyUiController = new DutyLootInDutyUiController {
            OnButtonClicked = addonDutyLoot.Toggle,
        };
        inDutyUiController.OnEnable();
    }

    public override void OnDisable() {
        journalUiController?.OnDisable();
        journalUiController = null;

        inDutyUiController?.OnDisable();
        inDutyUiController = null;

        addonDutyLoot?.Dispose();
        addonDutyLoot = null;

        config = null;
    }
}
