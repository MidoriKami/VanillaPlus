using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using VanillaPlus.Features.DutyLootPreview.Data;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Features.DutyLootPreview.Enums;
using VanillaPlus.Features.DutyLootPreview.Nodes;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// The window that shows loot for a duty.
/// </summary>
public class DutyLootPreviewAddon : NativeAddon {
    private const int VisibleItemCount = 12;
    internal const float ItemHeight = 32.0f;
    private const float ItemSpacing = 2.25f;
    private const float FilterBarHeight = 36.0f;
    private const float SeparatorHeight = 4.0f;
    private const float WindowOverhead = 67.75f;

    private const float ListAreaHeight = VisibleItemCount * ItemHeight + (VisibleItemCount - 1) * ItemSpacing;
    internal const float WindowHeight = ListAreaHeight + FilterBarHeight + SeparatorHeight + ItemSpacing + WindowOverhead;

    private DutyLootFilterBarNode? filterBarNode;
    private HorizontalLineNode? separatorNode;
    private ListNode<DutyLootItemView, DutyLootNode>? listNode;
    private TextNode? hintTextNode;

    public required DutyLootPreviewConfig Config { get; init; }
    public required DutyLootDataLoader DataLoader { get; init; }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        DataLoader.OnChanged += OnDataLoaderStateChanged;

        filterBarNode = new DutyLootFilterBarNode {
            Position = ContentStartPosition,
            Size = new Vector2(ContentSize.X, FilterBarHeight),
            OnFilterChanged = _ => Task.Run(UpdateList),
        };
        filterBarNode.AttachNode(this);

        separatorNode = new HorizontalLineNode {
            Position = ContentStartPosition + new Vector2(0, FilterBarHeight),
            Size = new Vector2(ContentSize.X, SeparatorHeight),
        };
        separatorNode.AttachNode(this);

        var listAreaPosition = ContentStartPosition + new Vector2(0, FilterBarHeight + SeparatorHeight + ItemSpacing);
        var listAreaSize = ContentSize - new Vector2(0, FilterBarHeight + SeparatorHeight + ItemSpacing);

        listNode = new ListNode<DutyLootItemView, DutyLootNode> {
            Position = listAreaPosition,
            Size = listAreaSize,
            OptionsList = [],
            ItemSpacing = ItemSpacing,
        };
        listNode.AttachNode(this);

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

        Task.Run(UpdateList);
    }

    private void OnDataLoaderStateChanged()
        => Task.Run(UpdateList);

    protected override unsafe void OnFinalize(AtkUnitBase* addon)
        => DataLoader.OnChanged -= OnDataLoaderStateChanged;

    private async Task UpdateList() {
        if (listNode is null || hintTextNode is null || filterBarNode is null || separatorNode is null) return;

        var dutyLootData = DataLoader.ActiveDutyLootData;
        if (dutyLootData is null && !DataLoader.IsLoading) {
            await CloseAsync();
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

        await Services.Framework.Run(() => listNode.OptionsList = viewModels);
        listNode.ResetScroll();

        var hasData = items.Count != 0;
        filterBarNode.IsVisible = hasData;
        separatorNode.IsVisible = hasData;

        var hasResults = viewModels.Count > 0;
        listNode.IsVisible = hasResults;
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
