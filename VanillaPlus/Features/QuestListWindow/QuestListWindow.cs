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
        DisplayName = Strings("ModificationDisplay_QuestListWindow"),
        Description = Strings("ModificationDescription_QuestListWindow"),
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

    private static string filterTypeLabel => Strings("QuestListWindow_FilterType");
    private static string filterAlphabeticallyLabel => Strings("QuestListWindow_FilterAlphabetically");
    private static string filterLevelLabel => Strings("QuestListWindow_FilterLevel");
    private static string filterDistanceLabel => Strings("QuestListWindow_FilterDistance");
    private static string filterIssuerNameLabel => Strings("QuestListWindow_FilterIssuerName");
    public override string ImageName => "QuestList.png";

    public override void OnEnable() {
        addonQuestList = new SearchableNodeListAddon {
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "QuestList",
            Title = Strings("QuestListWindow_Title"),
            UpdateListFunction = UpdateList,
            DropDownOptions = [ filterTypeLabel, filterAlphabeticallyLabel, filterLevelLabel, filterDistanceLabel, filterIssuerNameLabel ],
            OnFilterUpdated = OnFilterUpdated,
            OnSearchUpdated = OnSearchUpdated,
            OpenCommand = "/questlist",
        };

        addonQuestList.Initialize();
        
        OnFilterUpdated(filterTypeLabel, false);

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

    private bool UpdateList(VerticalListNode listNode, bool isOpening) {
        var filteredInventoryItems = GetQuests()
            .Where(item => item.IsRegexMatch(searchString))
            .ToList();

        var listUpdated = listNode.SyncWithListData(filteredInventoryItems, node => node.QuestInfo, data => new QuestEntryNode {
            Size = new Vector2(listNode.Width, 48.0f),
            QuestInfo = data,
        });

        if (listUpdated || updateRequested || filterString == filterDistanceLabel) {
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
            var s when s == filterAlphabeticallyLabel => string.CompareOrdinal(leftQuest.Name.ToString(), rightQuest.Name.ToString()),
            var s when s == filterTypeLabel => rightQuest.IconId.CompareTo(leftQuest.IconId),
            var s when s == filterLevelLabel => rightQuest.Level.CompareTo(leftQuest.Level),
            var s when s == filterDistanceLabel => leftQuest.Distance.CompareTo(rightQuest.Distance),
            var s when s == filterIssuerNameLabel => string.CompareOrdinal(leftQuest.IssuerName.ToString(), rightQuest.IssuerName.ToString()),
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
