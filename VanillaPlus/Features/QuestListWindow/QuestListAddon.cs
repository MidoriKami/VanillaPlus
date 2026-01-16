using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.Enums;
using VanillaPlus.NativeElements.Addons;
using Map = FFXIVClientStructs.FFXIV.Client.Game.UI.Map;

namespace VanillaPlus.Features.QuestListWindow;

public unsafe class QuestListAddon : SearchableNodeListAddon<MarkerInfo, QuestListItemNode> {
    private int? lastQuestCount;
    private QuestFilterMode lastSortingMode = QuestFilterMode.Type;
    private bool isReversed;

    public QuestListAddon() {
        OnSortingUpdated = UpdateSorting;
        OnSearchUpdated = UpdateSearch;
    }

    protected override void OnUpdate(AtkUnitBase* addon) {
        if (lastQuestCount != Map.Instance()->UnacceptedQuestMarkers.Count) {
            ListNode?.OptionsList = Map.Instance()->UnacceptedQuestMarkers.ToList();

            lastQuestCount = Map.Instance()->UnacceptedQuestMarkers.Count;
        }

        ListItems.Sort((left, right) => MarkerInfoExtensions.Comparison(left, right, lastSortingMode) * (isReversed ? -1 : 1));
        
        base.OnUpdate(addon);
    }

    protected override void OnFinalize(AtkUnitBase* addon) {
        base.OnFinalize(addon);

        lastQuestCount = null;
    }

    private void UpdateSorting(string newFilterString, bool reversed) {
        var enumValue = newFilterString.ParseAsEnum(QuestFilterMode.Type);

        lastSortingMode = enumValue;
        isReversed = reversed;
    }

    private void UpdateSearch(string newSearchString) {
        ListItems = Map.Instance()->UnacceptedQuestMarkers
            .Where(item => MarkerInfoExtensions.IsRegexMatch(item, newSearchString))
            .ToList();
        
        ListItems.Sort((left, right) => MarkerInfoExtensions.Comparison(left, right, lastSortingMode) * (isReversed ? -1 : 1));
    }
}
