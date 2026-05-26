using System.Numerics;
using System.Threading.Tasks;
using VanillaPlus.Classes;
using VanillaPlus.Features.DutyLootPreview.Data;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.DutyLootPreview;

public class DutyLootPreview : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_DutyLootPreview,
        Description = Strings.ModificationDescription_DutyLootPreview,
        Type = ModificationType.NewWindow,
        Authors = ["GrittyFrog"],
    };

    public override string ImageName => "DutyLootPreview.png";

    private DutyLootPreviewConfig? config;
    private DutyLootDataLoader? dataLoader;
    private DutyLootJournalUiController? journalUiController;
    private DutyLootInDutyUiController? inDutyUiController;
    private DutyLootPreviewAddon? addonDutyLoot;

    public override async Task OnEnableAsync() {
        config = await DutyLootPreviewConfig.Load();

        dataLoader = new DutyLootDataLoader();
        await dataLoader.EnableAsync();

        addonDutyLoot = new DutyLootPreviewAddon {
            InternalName = "DutyLootPreview",
            Title = Strings.Title_DutyLootPreview,
            Size = new Vector2(350.0f, DutyLootPreviewAddon.WindowHeight),
            Config = config,
            DataLoader = dataLoader,
        };

        journalUiController = new DutyLootJournalUiController {
            DataLoader = dataLoader,
            OnButtonClicked = addonDutyLoot.Toggle,
        };

        await journalUiController.EnableAsync();

        inDutyUiController = new DutyLootInDutyUiController {
            DataLoader = dataLoader,
            OnButtonClicked = addonDutyLoot.Toggle,
        };

        await inDutyUiController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        if (journalUiController is not null) {
            await journalUiController.DisableAsync();
            journalUiController = null;
        }

        if (inDutyUiController is not null) {
            await inDutyUiController.DisableAsync();
            inDutyUiController = null;
        }

        if (addonDutyLoot is not null) {
            await addonDutyLoot.DisposeAsync();
            addonDutyLoot = null;
        }

        if (dataLoader is not null) {
            await dataLoader.DisposeAsync();
            dataLoader = null;
        }

        config = null;
    }
}
