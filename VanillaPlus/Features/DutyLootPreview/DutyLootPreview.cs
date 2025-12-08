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
        ],
    };

    private DutyLootUiHook? uiHook;
    private DutyLootPreviewAddon? addonDutyLoot;

    public override void OnEnable() {
        addonDutyLoot = DutyLootPreviewAddon.Create();

        uiHook = new DutyLootUiHook {
            OnButtonClicked = addonDutyLoot.Toggle,
        };
        uiHook.OnEnable();
    }

    public override void OnDisable() {
        uiHook?.OnDisable();
        uiHook = null;

        addonDutyLoot?.Dispose();
        addonDutyLoot = null;
    }
}
