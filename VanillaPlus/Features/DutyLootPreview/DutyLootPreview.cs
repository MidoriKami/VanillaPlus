using System.Linq;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DutyLootPreview;

public unsafe partial class DutyLootPreview : GameModification {
    private DutyLootUIHook UIHook = new();
    private DutyLootPreviewAddon? AddonDutyLoot;

    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Duty Loot Preview",
        Description = "Adds a duty loot viewer to the duty window",
        Type = ModificationType.UserInterface,
        Authors = [ "GrittyFrog" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override void OnEnable() {
        UIHook.OnEnable();
        UIHook.ShowDutyLootPreviewButtonClicked += OnShowDutyLootPreviewButtonClicked;
        UIHook.DutyChanged += OnDutyChanged;

        AddonDutyLoot = DutyLootPreviewAddon.Create();
    }

    public override void OnDisable() {
        UIHook.OnDisable();

        AddonDutyLoot?.Dispose();
        AddonDutyLoot = null;
    }

    private void OnDutyChanged(uint? contentFinderConditionId) {
        if (!contentFinderConditionId.HasValue) {
            AddonDutyLoot?.Clear();
            return;
        }

        var items = DutyLootItem.ForContent(contentFinderConditionId.Value)
            .OrderBy(item => item.ItemSortCategory == 5 || item.ItemSortCategory == 56 ? uint.MaxValue : item.ItemSortCategory)
            .ToList();
        AddonDutyLoot?.SetItems(items);
    }

    private void OnShowDutyLootPreviewButtonClicked() {
        AddonDutyLoot?.Toggle();
    }
}
