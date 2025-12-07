using System.Linq;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DutyLootPreview;

public class DutyLootPreview : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Duty Loot Preview",
        Description = "Adds a duty loot viewer to the duty window",
        Type = ModificationType.UserInterface,
        Authors = [ "GrittyFrog" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };
    
    private DutyLootUiHook? uiHook;
    private DutyLootPreviewAddon? addonDutyLoot;

    public override void OnEnable() {
        uiHook = new DutyLootUiHook {
            OnShowDutyLootPreviewButtonClicked = OnShowDutyLootPreviewButtonClicked,
            DutyChanged = OnDutyChanged,
        };
        uiHook.OnEnable();

        addonDutyLoot = DutyLootPreviewAddon.Create();
    }

    public override void OnDisable() {
        uiHook?.OnDisable();
        uiHook = null;

        addonDutyLoot?.Dispose();
        addonDutyLoot = null;
    }

    private void OnDutyChanged(uint? contentFinderConditionId) {
        if (!contentFinderConditionId.HasValue) {
            addonDutyLoot?.Clear();
            return;
        }

        var items = DutyLootItem.ForContent(contentFinderConditionId.Value)
            .OrderBy(item => item.ItemSortCategory is 5 or 56 ? uint.MaxValue : item.ItemSortCategory)
            .ToList();
        addonDutyLoot?.SetItems(items);
    }

    private void OnShowDutyLootPreviewButtonClicked() {
        addonDutyLoot?.Toggle();
    }
}
