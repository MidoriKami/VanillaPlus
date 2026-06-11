using System;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Nodes;
using Lumina.Data.Parsing.Uld;
using Lumina.Text.ReadOnly;
using VanillaPlus.Features.QuestListWindow.Nodes;

namespace VanillaPlus.Features.QuestListWindow.Addons;

public class QuestListAddon : NativeAddon {

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        new VerticalListNode {
            Position = ContentStartPosition,
            Size = ContentSize,
            FitWidth = true,
            ItemSpacing = 4.0f,
            InitialNodes = [
                new TextInputNode {
                    Height = 26.0f,
                    PlaceholderStringId = 325, // "Search"
                    SheetType = NodeData.SheetType.Addon,
                    OnInputReceived = OnSearchUpdated,
                },
                listNode = new ListNode<MarkerInfo, QuestListItemNode> {
                    Height = ContentSize.Y - 26.0f - 4.0f,
                    AutoResetScroll = false,
                    ItemSpacing = 3.0f,
                    OptionsList = [],
                },
            ],
        }.AttachNode(this);
    }

    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        listNode?.OptionsList = Map.Instance()->UnacceptedQuestMarkers
            .Where(item => lastSearchString == string.Empty || MarkerInfoExtensions.IsRegexMatch(item, lastSearchString))
            .OrderBy(item => item.Distance)
            .ToList();

        listNode?.Update();
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        base.OnFinalize(addon);

        listNode = null;
    }

    private void OnSearchUpdated(ReadOnlySeString searchString) {
        lastSearchString = searchString.ToString();
        listNode?.ResetScroll();
    }

    private ListNode<MarkerInfo, QuestListItemNode>? listNode;
    private string lastSearchString = string.Empty;

    // private QuestFilterMode lastSortingMode = QuestFilterMode.Type;
    // private bool isReversed;
    // private string searchString = string.Empty;
    //
    // public QuestListAddon() {
    //     OnSortingUpdated = UpdateSorting;
    //     OnSearchUpdated = UpdateSearch;
    // }
    //
    // protected override void OnUpdate(AtkUnitBase* addon) {
    //     ListNode?.OptionsList = Map.Instance()->UnacceptedQuestMarkers
    //         .Where(item => searchString == string.Empty || MarkerInfoExtensions.IsRegexMatch(item, searchString))
    //         .ToList();
    //
    //     ListNode?.OptionsList
    //         .Sort((left, right) => MarkerInfoExtensions.Comparison(left, right, lastSortingMode) * (isReversed ? -1 : 1));
    //
    //     base.OnUpdate(addon);
    // }
    //
    // private void UpdateSorting(Enum sortingMode, bool reversed) {
    //     lastSortingMode = (QuestFilterMode)sortingMode;
    //     isReversed = reversed;
    // }
    //
    // private void UpdateSearch(string newSearchString) {
    //     searchString = newSearchString;
    // }
}
