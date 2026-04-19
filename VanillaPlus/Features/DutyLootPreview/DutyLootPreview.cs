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
