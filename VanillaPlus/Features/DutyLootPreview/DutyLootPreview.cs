using System.Numerics;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DutyLootPreview;

public class DutyLootPreview : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_DutyLootPreview,
        Description = Strings.ModificationDescription_DutyLootPreview,
        Type = ModificationType.NewWindow,
        Authors = [ "GrittyFrog" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Added Context Menu, Filter Buttons, Favorites and\nItem Drop info"),
            new ChangeLogInfo(3, "Added button to view current duty loot"),
            new ChangeLogInfo(4, "- Loot is now properly loaded when opening the\n  Duty Loot Preview from the Duty Journal\n- Unlocked items now show a checkmark")
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
            Title = Strings.Title_DutyLootPreview,
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
