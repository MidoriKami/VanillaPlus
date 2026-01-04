using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using KamiToolKit;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.Classes;
using VanillaPlus.NativeElements.Addons;
using Map = FFXIVClientStructs.FFXIV.Client.Game.UI.Map;

namespace VanillaPlus.Features.QuestListWindow;

public unsafe class QuestListWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_QuestListWindow,
        Description = Strings.ModificationDescription_QuestListWindow,
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private SearchableNodeListAddon? addonQuestList;
    private string filterString = string.Empty;
    private string searchString = string.Empty;
    private bool filterReversed;
    private bool updateRequested;

    private static string FilterTypeLabel => Strings.QuestListWindow_FilterType;
    private static string FilterAlphabeticallyLabel => Strings.QuestListWindow_FilterAlphabetically;
    private static string FilterLevelLabel => Strings.QuestListWindow_FilterLevel;
    private static string FilterDistanceLabel => Strings.QuestListWindow_FilterDistance;
    private static string FilterIssuerNameLabel => Strings.QuestListWindow_FilterIssuerName;
    public override string ImageName => "QuestList.png";

    public override void OnEnable() {
        addonQuestList = new SearchableNodeListAddon {
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "QuestList",
            Title = Strings.QuestListWindow_Title,
            UpdateListFunction = UpdateList,
            DropDownOptions = [ FilterTypeLabel, FilterAlphabeticallyLabel, FilterLevelLabel, FilterDistanceLabel, FilterIssuerNameLabel ],
            OnFilterUpdated = OnFilterUpdated,
            OnSearchUpdated = OnSearchUpdated,
            OpenCommand = "/questlist",
        };

        addonQuestList.Initialize();
        
        OnFilterUpdated(FilterTypeLabel, false);

        OpenConfigAction = addonQuestList.OpenAddonConfig;
    }

    public override void OnDisable() {
        addonQuestList?.Dispose();
        addonQuestList = null;
    }

    private void OnFilterUpdated(string newFilterString, bool reversed) {
        updateRequested = true;
        filterString = newFilterString;
        filterReversed = reversed;
        addonQuestList?.DoListUpdate();
    }

    private void OnSearchUpdated(string newSearchString) {
        updateRequested = true;
        searchString = newSearchString;
        addonQuestList?.DoListUpdate();
    }

    private bool UpdateList(ScrollingListNode listNode, bool isOpening) {
        var filteredInventoryItems = GetQuests()
            .Where(item => item.IsRegexMatch(searchString))
            .ToList();

        var listUpdated = listNode.SyncWithListData(filteredInventoryItems, node => node.QuestInfo, data => new QuestEntryNode {
            Size = new Vector2(listNode.ContentWidth, 48.0f),
            Height = 48.0f,
            QuestInfo = data,
        });

        if (listUpdated || updateRequested || filterString == FilterDistanceLabel) {
            listNode.ReorderNodes(Comparison);
        }

        foreach (var questNode in listNode.GetNodes<QuestEntryNode>()) {
            questNode.Update();
        }
        
        updateRequested = false;
        return listUpdated;
    }

    private int Comparison(NodeBase x, NodeBase y) {
        if (x is not QuestEntryNode left || y is not QuestEntryNode right) return 0;

        var leftQuest = left.QuestInfo;
        var rightQuest = right.QuestInfo;

        var result = filterString switch {
            var s when s == FilterAlphabeticallyLabel => string.CompareOrdinal(leftQuest.Name.ToString(), rightQuest.Name.ToString()),
            var s when s == FilterTypeLabel => rightQuest.IconId.CompareTo(leftQuest.IconId),
            var s when s == FilterLevelLabel => rightQuest.Level.CompareTo(leftQuest.Level),
            var s when s == FilterDistanceLabel => leftQuest.Distance.CompareTo(rightQuest.Distance),
            var s when s == FilterIssuerNameLabel => string.CompareOrdinal(leftQuest.IssuerName.ToString(), rightQuest.IssuerName.ToString()),
            _ => string.CompareOrdinal(leftQuest.Name.ToString(), rightQuest.Name.ToString()),
        };

        var reverseModifier = filterReversed ? -1 : 1;
        
        return ( result is 0 ? string.CompareOrdinal(leftQuest.Name.ToString(), rightQuest.Name.ToString()) : result ) * reverseModifier;
    }
    
    private static List<QuestInfo> GetQuests() {

        List<QuestInfo> quests = [];
        foreach (var questMarker in Map.Instance()->UnacceptedQuestMarkers) {
            if (questMarker.ObjectiveId is 0) continue;
            
            var questInfo = Services.DataManager.GetExcelSheet<Quest>().GetRow(questMarker.ObjectiveId + ushort.MaxValue + 1);

            var newQuestInfo = new QuestInfo{
                ObjectiveId = questMarker.ObjectiveId,
                IconId = questMarker.MarkerData.First->IconId, 
                Name = questMarker.Label.AsSpan(),
                Level = questInfo.ClassJobLevel.First(),
                Position = questMarker.MarkerData.First->Position,
                IssuerName = questInfo.IssuerStart.GetValueOrDefault<ENpcResident>()?.Singular ?? string.Empty,
            };

            quests.Add(newQuestInfo);
        }

        return quests;
    }
}
