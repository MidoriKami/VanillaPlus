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
            OnButtonClicked = () => Task.Run(addonDutyLoot.ToggleAsync),
        };

        inDutyUiController = new DutyLootInDutyUiController {
            DataLoader = dataLoader,
            OnButtonClicked = () => Task.Run(addonDutyLoot.ToggleAsync),
        };

        await Services.Framework.Run(() => {
            journalUiController.Enable();
            inDutyUiController.Enable();
        });
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.Run(() => {
            journalUiController?.Dispose();
            inDutyUiController?.Dispose();
        });

        journalUiController = null;
        inDutyUiController = null;

        await Task.WhenAll(
            addonDutyLoot?.DisposeAsync().AsTask() ?? Task.CompletedTask,
            dataLoader?.DisposeAsync().AsTask() ?? Task.CompletedTask
        );

        addonDutyLoot = null;
        dataLoader = null;

        config = null;
    }
}
