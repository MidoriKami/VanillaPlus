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
    internal const int VisibleItemCount = 10;
    internal const float ItemHeight = 36.0f;
    internal const float ItemSpacing = 2.25f;
    internal const float FilterBarHeight = 36.0f;
    internal const float SeparatorHeight = 4.0f;
    private const float WindowOverhead = 67.75f;

    private const float ListAreaHeight = VisibleItemCount * ItemHeight + (VisibleItemCount - 1) * ItemSpacing;
    internal const float WindowHeight = ListAreaHeight + FilterBarHeight + SeparatorHeight + ItemSpacing + WindowOverhead;

    private DutyLootFilterBarNode? filterBarNode;
    private HorizontalLineNode? separatorNode;
    private ListNode<DutyLootItemView, DutyLootNode>? scrollingAreaNode;
    private TextNode? hintTextNode;

    public required DutyLootPreviewConfig Config { get; init; }
    public required DutyLootDataLoader DataLoader { get; init; }

    protected override void OnSetup(AtkUnitBase* addon) {
        DataLoader.OnChanged += OnDataLoaderStateChanged;

        filterBarNode = new DutyLootFilterBarNode {
            Position = ContentStartPosition,
            Size = new Vector2(ContentSize.X, FilterBarHeight),
            OnFilterChanged = _ => UpdateList(),
        };
        filterBarNode.AttachNode(this);

        separatorNode = new HorizontalLineNode {
            Position = ContentStartPosition + new Vector2(0, FilterBarHeight),
            Size = new Vector2(ContentSize.X, SeparatorHeight),
        };
        separatorNode.AttachNode(this);

        var listAreaPosition = ContentStartPosition + new Vector2(0, FilterBarHeight + SeparatorHeight + ItemSpacing);
        var listAreaSize = ContentSize - new Vector2(0, FilterBarHeight + SeparatorHeight + ItemSpacing);

        scrollingAreaNode = new ListNode<DutyLootItemView, DutyLootNode> {
            Position = listAreaPosition,
            Size = listAreaSize,
            OptionsList = [],
            ItemSpacing = ItemSpacing,
        };
        scrollingAreaNode.AttachNode(this);

        hintTextNode = new TextNode {
            Position = ContentStartPosition,
            Size = ContentSize,
            TextColor = ColorHelper.GetColor(1),
            LineSpacing = 18,
            TextFlags = TextFlags.MultiLine | TextFlags.Edge | TextFlags.WordWrap,
            AlignmentType = AlignmentType.Center,
            String = Strings.DutyLoot_NoItemsMessage,
        };
        UpdateHintTextNodePosition();
        hintTextNode.AttachNode(this);

        UpdateList();
    }

    private void OnDataLoaderStateChanged()
        => Services.Framework.RunOnFrameworkThread(UpdateList);

    protected override void OnFinalize(AtkUnitBase* addon)
        => DataLoader.OnChanged -= OnDataLoaderStateChanged;

    private void UpdateList() {
        if (scrollingAreaNode is null || hintTextNode is null || filterBarNode is null || separatorNode is null) return;

        var dutyLootData = DataLoader.ActiveDutyLootData;
        if (dutyLootData is null && !DataLoader.IsLoading) {
            Close();
            return;
        }

        var items = dutyLootData?.Items ?? [];

        var filteredItems = filterBarNode.CurrentFilter switch {
            LootFilter.Favorites => items.Where(item => Config.FavoriteItems.Contains(item.ItemId)),
            LootFilter.Equipment => items.Where(item => item.IsEquipment),
            LootFilter.Misc => items.Where(item => !item.IsEquipment),
            _ => items,
        };

        var viewModels = filteredItems
            .Order()
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
                _ when DataLoader.IsLoading => Strings.DutyLoot_LoadingMessage,
                _ when hasData => Strings.DutyLoot_NoResultsMessage,
                _ => Strings.DutyLoot_NoItemsMessage,
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
