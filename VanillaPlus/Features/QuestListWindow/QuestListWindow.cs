using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using KamiToolKit.Nodes;
using KamiToolKit.System;
using VanillaPlus.Basic_Addons;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.QuestListWindow;

public unsafe class QuestListWindow : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Quest List Window",
        Description = "Displays a list of all available quests for the currently occupied zone.",
        Type = ModificationType.NewWindow,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private SearchableNodeListAddon? addonQuestList;
    private string filterString = string.Empty;
    private string searchString = string.Empty;
    private bool filterReversed;
    private bool updateRequested;

    public override void OnEnable() {
        addonQuestList = new SearchableNodeListAddon {
            NativeController = System.NativeController,
            Size = new Vector2(300.0f, 400.0f),
            InternalName = "QuestList",
            Title = "Quest List",
            UpdateListFunction = UpdateList,
            DropDownOptions = ["Alphabetically", "Level", "Type" ],
            OnFilterUpdated = OnFilterUpdated,
            OnSearchUpdated = OnSearchUpdated,
            OpenCommand = "/questlist",
        };
        
        addonQuestList.Initialize([VirtualKey.MENU, VirtualKey.CONTROL, VirtualKey.J]);

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
            Size = new Vector2(listNode.Width, 32.0f),
            QuestInfo = data,
            IsVisible = true,
        });

        if (listUpdated || updateRequested) {
            listNode.ReorderNodes(Comparison);
        }
        
        updateRequested = false;
        return listUpdated;
    }

    private int Comparison(NodeBase x, NodeBase y) {
        if (x is not QuestEntryNode left || y is not QuestEntryNode right) return 0;

        var leftQuest = left.QuestInfo;
        var rightQuest = right.QuestInfo;

        var result = filterString switch {
            "Alphabetically" => string.CompareOrdinal(leftQuest.Name.ToString(), rightQuest.Name.ToString()),
            "Type" => rightQuest.MarkerData.IconId.CompareTo(leftQuest.MarkerData.IconId),
            "Level" => rightQuest.Level.CompareTo(leftQuest.Level),
            _ => string.CompareOrdinal(leftQuest.Name.ToString(), rightQuest.Name.ToString()),
        };

        var reverseModifier = filterReversed ? -1 : 1;
        
        return ( result is 0 ? string.CompareOrdinal(leftQuest.Name.ToString(), rightQuest.Name.ToString()) : result ) * reverseModifier;
    }
    
    private static List<QuestInfo> GetQuests() {

        List<QuestInfo> quests = [];
        quests.AddRange(from questMarker in Map.Instance()->UnacceptedQuestMarkers 
                        where questMarker is not { ObjectiveId: 0 } 
                        select new QuestInfo(
                            questMarker.MarkerData.First->IconId, 
                            questMarker.Label.AsSpan(), 
                            questMarker.RecommendedLevel,
                            *questMarker.MarkerData.First));

        return quests;
    }
}
