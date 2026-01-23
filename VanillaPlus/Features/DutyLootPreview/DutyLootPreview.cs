using System.Numerics;
using VanillaPlus.Classes;
using VanillaPlus.Features.DutyLootPreview.Data;
using VanillaPlus.Enums;

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
            new ChangeLogInfo(4, "- Loot is now properly loaded when opening the\n  Duty Loot Preview from the Duty Journal\n- Unlocked items now show a checkmark"),
            new ChangeLogInfo(5, "- Arm Containers now appear under Equipment\n- In Duty button no longer appears above all windows"),
            new ChangeLogInfo(6, "- Fix crash when changing filters\n- Now indicates when all unlockables are collected from a Duty")
        ],
    };

    public override string ImageName => "DutyLootPreview.png";

    private DutyLootPreviewConfig? config;
    private DutyLootDataLoader? dataLoader;
    private DutyLootJournalUiController? journalUiController;
    private DutyLootInDutyUiController? inDutyUiController;
    private DutyLootPreviewAddon? addonDutyLoot;

    public override void OnEnable() {
        config = DutyLootPreviewConfig.Load();

        dataLoader = new DutyLootDataLoader();
        dataLoader.Enable();

        addonDutyLoot = new DutyLootPreviewAddon {
            InternalName = "DutyLootPreview",
            Title = Strings.Title_DutyLootPreview,
            Size = new Vector2(300.0f, DutyLootPreviewAddon.WindowHeight),
            Config = config,
            DataLoader = dataLoader,
        };

        journalUiController = new DutyLootJournalUiController {
            DataLoader = dataLoader,
            OnButtonClicked = addonDutyLoot.Toggle,
        };
        journalUiController.OnEnable();

        inDutyUiController = new DutyLootInDutyUiController {
            DataLoader = dataLoader,
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

        dataLoader?.Dispose();
        dataLoader = null;

        config = null;
    }
}
