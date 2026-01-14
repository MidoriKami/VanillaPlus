using System.Linq;
using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
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

        ListItems.Sort((left, right) => Comparison(left, right, lastSortingMode) * (isReversed ? -1 : 1));
        
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
            .Where(item => IsRegexMatch(item, newSearchString))
            .ToList();
        
        ListItems.Sort((left, right) => Comparison(left, right, lastSortingMode) * (isReversed ? -1 : 1));
    }
    
    private static bool IsRegexMatch(MarkerInfo marker, string searchString) {
        if (!Services.DataManager.GetExcelSheet<Quest>().TryGetRow(marker.ObjectiveId + ushort.MaxValue + 1, out var questInfo)) return false;
        if (questInfo.RowId is 0) return false;
        
        var regex = new Regex(searchString, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    
        if (regex.IsMatch(questInfo.Name.ToString())) return true;
        if (regex.IsMatch(questInfo.ClassJobLevel.First().ToString())) return true;
        if (regex.IsMatch(questInfo.IssuerStart.GetValueOrDefault<ENpcResident>()?.Singular.ToString() ?? string.Empty)) return true;
    
        return false;
    }
    
    private static int Comparison(MarkerInfo left, MarkerInfo right, QuestFilterMode filterMode) {
        var result = filterMode switch {
            QuestFilterMode.Alphabetically => string.CompareOrdinal(left.Name, right.Name),
            QuestFilterMode.Type => left.IconId.CompareTo(right.IconId),
            QuestFilterMode.ClassJobLevel => left.ClassJobLevel.CompareTo(right.ClassJobLevel),
            QuestFilterMode.IssuerName => string.CompareOrdinal(left.IssuerName, right.IssuerName),
            QuestFilterMode.Distance => left.Distance.CompareTo(right.Distance),
            _ => string.CompareOrdinal(left.Name, right.Name),
        };
    
        return result is 0 ? string.CompareOrdinal(left.Name, right.Name) : result;
    }
}
