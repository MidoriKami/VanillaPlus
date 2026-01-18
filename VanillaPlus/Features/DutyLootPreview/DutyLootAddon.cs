using System.Linq;
using System.Numerics;
using VanillaPlus.Features.DutyLootPreview.Data;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Features.DutyLootPreview.Nodes;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// The window that shows loot for a duty.
/// </summary>
public unsafe class DutyLootPreviewAddon : NativeAddon {
    private static string NoItemsMessage => Strings.DutyLoot_NoItemsMessage;
    private static string NoResultsMessage => Strings.DutyLoot_NoResultsMessage;
    private static string LoadingMessage => Strings.DutyLoot_LoadingMessage;

    private DutyLootFilterBarNode? filterBarNode;
    private HorizontalLineNode? separatorNode;
    private ListNode<DutyLootItemView, DutyLootNode>? scrollingAreaNode;
    private TextNode? hintTextNode;

    public required DutyLootPreviewConfig Config { get; init; }
    public required DutyLootDataLoader DataLoader { get; init; }

    protected override void OnSetup(AtkUnitBase* addon) {
        const float filterBarHeight = 36f;
        const float separatorHeight = 4f;

        DataLoader.OnDutyLootDataChanged += OnDataLoaderStateChanged;

        filterBarNode = new DutyLootFilterBarNode {
            Position = ContentStartPosition,
            Size = new Vector2(ContentSize.X, filterBarHeight),
            OnFilterChanged = _ => UpdateList(),
        };
        filterBarNode.AttachNode(this);

        separatorNode = new HorizontalLineNode {
            Position = ContentStartPosition + new Vector2(0, filterBarHeight),
            Size = new Vector2(ContentSize.X, 4.0f),
        };
        separatorNode.AttachNode(this);

        var listAreaPosition = ContentStartPosition + new Vector2(0, filterBarHeight + separatorHeight);
        var listAreaSize = ContentSize - new Vector2(0, filterBarHeight + separatorHeight);

        scrollingAreaNode = new ListNode<DutyLootItemView, DutyLootNode> {
            Position = listAreaPosition,
            Size = listAreaSize,
            OptionsList = [],
        };
        scrollingAreaNode.AttachNode(this);

        hintTextNode = new TextNode {
            Position = ContentStartPosition,
            Size = ContentSize,
            TextColor = ColorHelper.GetColor(1),
            LineSpacing = 18,
            TextFlags = TextFlags.MultiLine | TextFlags.Edge | TextFlags.WordWrap,
            AlignmentType = AlignmentType.Center,
            String = NoItemsMessage,
        };
        UpdateHintTextNodePosition();
        hintTextNode.AttachNode(this);

        UpdateList();
    }

    private void OnDataLoaderStateChanged(DutyLootData state) {
        Services.Framework.RunOnFrameworkThread(() => UpdateList());
    }

    protected override void OnFinalize(AtkUnitBase* addon) {
        DataLoader.OnDutyLootDataChanged -= OnDataLoaderStateChanged;
    }

    private void UpdateList() {
        if (scrollingAreaNode is null || hintTextNode is null || filterBarNode is null || separatorNode is null) return;

        var state = DataLoader.CurrentDutyLootData;

        // Close if no valid content
        if (state.ContentId is null && !state.IsLoading) {
            Close();
            return;
        }

        var items = state.Items;
        var isLoading = state.IsLoading;

        var filteredItems = filterBarNode.CurrentFilter switch {
            LootFilter.Favorites => items.Where(item => Config.FavoriteItems.Contains(item.ItemId)),
            LootFilter.Equipment => items.Where(item => item.IsEquipment),
            LootFilter.Misc => items.Where(item => !item.IsEquipment),
            _ => items,
        };

        var viewModels = filteredItems
            .Select(item => new DutyLootItemView(
                Item: item,
                IsFavorite: Config.FavoriteItems.Contains(item.ItemId),
                Config: Config
            ))
            .ToList();

        scrollingAreaNode.OptionsList = viewModels;

        var hasData = items.Any();
        filterBarNode.IsVisible = hasData;
        separatorNode.IsVisible = hasData;

        var hasResults = viewModels.Count > 0;
        scrollingAreaNode.IsVisible = hasResults;
        hintTextNode.IsVisible = !hasResults;

        if (!hasResults) {
            hintTextNode.String = true switch {
                _ when isLoading => LoadingMessage,
                _ when hasData => NoResultsMessage,
                _ => NoItemsMessage,
            };
            UpdateHintTextNodePosition();
        }
    }

    private void UpdateHintTextNodePosition() {
        if (filterBarNode is null || separatorNode is null || hintTextNode is null) return;
        var offsetTop = 0f;
        if (filterBarNode.IsVisible) offsetTop += filterBarNode.Height;
        if (separatorNode.IsVisible) offsetTop += separatorNode.Height;
        hintTextNode.Size = hintTextNode.Size with { Y = ContentSize.Y - offsetTop };
        hintTextNode.Position = hintTextNode.Position with { Y = ContentStartPosition.Y + offsetTop };
    }
}
