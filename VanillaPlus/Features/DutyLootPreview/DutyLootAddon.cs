using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Classes.ContextMenu;
using KamiToolKit.Classes.Controllers;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// The window that shows loot for a duty.
/// </summary>
public unsafe class DutyLootPreviewAddon : NativeAddon {
    public static DutyLootPreviewAddon Create() {
        return new DutyLootPreviewAddon {
            InternalName = "DutyLootPreview",
            Title = "Duty Loot Preview",
            Size = new Vector2(300.0f, 350.0f),
        };
    }

    private const string NoItemsMessage = "No loot data found for this duty.\n\nData is provided by a third party and may be incomplete.";
    private const string NoResultsMessage = "No results";
    private const string LoadingMessage = "Loading loot data...";

    private DutyLootFilterBarNode? filterBarNode;
    private HorizontalLineNode? separatorNode;
    private ScrollingAreaNode<VerticalListNode>? scrollingAreaNode;
    private TextNode? noItemsTextNode;

    private bool updateRequested = true;
    private bool isLoading;
    private List<DutyLootItem> items = [];

    private AddonController<AddonContentsFinder>? contentsFinder;
    private CancellationTokenSource? loadingCts;
    private uint? lastLoadedContentId;
    private DutyLootPreviewConfig config = null!;

    public DutyLootPreviewAddon() {
        config = DutyLootPreviewConfig.Load();
    }

    internal void SetItems(IEnumerable<DutyLootItem> itemsEnumerable) {
        items = itemsEnumerable.ToList();
        isLoading = false;
        updateRequested = true;
    }

    private void Clear() {
        items = [];
        isLoading = false;
        updateRequested = true;
    }

    internal void SetLoading() {
        items = [];
        isLoading = true;
        updateRequested = true;
    }

    private static uint? GetCurrentContentId() {
        var content = AgentContentsFinder.Instance()->SelectedDuty;
        return content.ContentType == ContentsId.ContentsType.Regular ? content.Id : null;
    }

    private void LoadCurrentDuty() {
        loadingCts?.Cancel();
        loadingCts?.Dispose();
        loadingCts = null;

        var contentId = GetCurrentContentId();
        if (contentId is null) {
            Clear();
            lastLoadedContentId = null;
            return;
        }

        // Don't reload if we already have this duty loaded
        if (contentId == lastLoadedContentId) return;
        lastLoadedContentId = contentId;

        loadingCts = new CancellationTokenSource();
        var token = loadingCts.Token;
        var id = contentId.Value;

        Task.Run(() => this.LoadDutyItemsAsync(id, token), token);
    }

    private void OnContentsFinderUpdate(AddonContentsFinder* addon) {
        if (!IsOpen) return;

        var contentId = GetCurrentContentId();
        if (contentId != lastLoadedContentId) {
            LoadCurrentDuty();
        }
    }

    protected override void OnSetup(AtkUnitBase* addon) {
        const float filterBarHeight = 36f;
        const float separatorHeight = 4f;

        filterBarNode = new DutyLootFilterBarNode {
            Position = ContentStartPosition,
            Size = new Vector2(ContentSize.X, filterBarHeight),
            OnFilterChanged = _ => { updateRequested = true; }
        };
        filterBarNode.AttachNode(this);

        separatorNode = new HorizontalLineNode {
            Position = ContentStartPosition + new Vector2(0, filterBarHeight),
            Width = ContentSize.X,
            Height = 4
        };
        separatorNode.AttachNode(this);

        var listAreaPosition = ContentStartPosition + new Vector2(0, filterBarHeight + separatorHeight);
        var listAreaSize = ContentSize - new Vector2(0, filterBarHeight + separatorHeight);

        scrollingAreaNode = new ScrollingAreaNode<VerticalListNode> {
            Position = listAreaPosition,
            Size = listAreaSize,
            ContentHeight = 100,
        };
        scrollingAreaNode.ContentNode.FitContents = true;
        scrollingAreaNode.AttachNode(this);

        noItemsTextNode = new TextNode {
            Position = listAreaPosition,
            Size = listAreaSize,
            TextColor = ColorHelper.GetColor(1),
            LineSpacing = 18,
            TextFlags = TextFlags.MultiLine | TextFlags.Edge | TextFlags.WordWrap,
            AlignmentType = AlignmentType.Center,
            SeString = NoItemsMessage,
        };
        noItemsTextNode.AttachNode(this);

        contentsFinder = new AddonController<AddonContentsFinder>("ContentsFinder");
        contentsFinder.OnRefresh += OnContentsFinderUpdate;
        contentsFinder.OnDetach += _ => Close();
        contentsFinder.Enable();

        LoadCurrentDuty();
        UpdateList(true);
    }

    public override void Dispose() {
        base.Dispose();

        loadingCts?.Cancel();
        loadingCts?.Dispose();
        loadingCts = null;

        contentsFinder?.Dispose();
        contentsFinder = null;
    }

    protected override void OnUpdate(AtkUnitBase* addon) => UpdateList();

    private void UpdateList(bool isOpening = false) {
        if (scrollingAreaNode is null || noItemsTextNode is null || filterBarNode is null) return;
        if (!updateRequested && !isOpening) return;
        updateRequested = false;

        var filteredItems = filterBarNode.CurrentFilter switch {
            LootFilter.Favorites => items.Where(item => config.FavoriteItems.Contains(item.ItemId)),
            LootFilter.Equipment => items.Where(item => item.ItemSortCategory is 5 or 56),
            LootFilter.Misc => items.Where(item => item.ItemSortCategory is not (5 or 56)),
            _ => items
        };

        var sortedItems = filteredItems;

        var list = scrollingAreaNode.ContentNode;
        var listUpdated = list.SyncWithListData(
            sortedItems,
            node => node.Item,
            data => new DutyLootNode {
                Size = new Vector2(list.Width, 36.0f),
                Item = data,
                IsFavorite = config.FavoriteItems.Contains(data.ItemId),
                OnLeftClick = OnDutyLootItemLeftClick,
                OnRightClick = OnDutyLootItemRightClick
            }
        );

        if (listUpdated) {
            scrollingAreaNode.ScrollPosition = 0;
            scrollingAreaNode.ContentHeight = scrollingAreaNode.ContentNode.Nodes.Sum(node => node.IsVisible ? node.Height : 0.0f);

            list.ReorderNodes((a, b) => {
                if (a is not DutyLootNode left || b is not DutyLootNode right) return 0;
                return left.Item.SortOrder.CompareTo(right.Item.SortOrder);
            });
        }

        var hasData = items.Count > 0 && !isLoading;
        var hasResults = list.GetNodes<DutyLootNode>().Any();

        filterBarNode.IsVisible = hasData;
        separatorNode!.IsVisible = hasData;
        scrollingAreaNode.IsVisible = hasResults;
        noItemsTextNode.IsVisible = !hasResults;

        if (!hasResults) {
            noItemsTextNode.SeString = isLoading ? LoadingMessage : hasData ? NoResultsMessage : NoItemsMessage;
        }
    }

    private void UpdateFavoriteStars() {
        if (scrollingAreaNode is null) return;

        foreach (var node in scrollingAreaNode.ContentNode.GetNodes<DutyLootNode>()) {
            node.IsFavorite = config.FavoriteItems.Contains(node.Item.ItemId);
        }
    }

    private void OnDutyLootItemLeftClick(DutyLootItem item) {
        if (item.CanTryOn) {
            AgentTryon.TryOn(0, item.ItemId);
        }
    }

    private void OnDutyLootItemRightClick(DutyLootItem item) {
        var contextMenuItems = new List<ContextMenuItem>();
        if (item.CanTryOn) {
            contextMenuItems.Add(new ContextMenuItem {
                Name = "Try On",
                OnClick = () => { AgentTryon.TryOn(0, item.ItemId); }
            });
        }
        var isFavorite = config.FavoriteItems.Contains(item.ItemId);
        contextMenuItems.Add(new ContextMenuItem {
            Name = isFavorite ? "Remove from Favorites" : "Add to Favorites",
            OnClick = () => {
                if (isFavorite) {
                    config.FavoriteItems.Remove(item.ItemId);
                } else {
                    config.FavoriteItems.Add(item.ItemId);
                }
                config.Save();
                UpdateFavoriteStars();
            }
        });
        contextMenuItems.Add(new ContextMenuItem {
            Name = "Search for Item",
            OnClick = () => {
                ItemFinderModule.Instance()->SearchForItem(item.ItemId);
            }
        });
        contextMenuItems.Add(new ContextMenuItem {
            Name = "Link",
            OnClick = () => {
                AgentChatLog.Instance()->LinkItem(item.ItemId);
            }
        });
        contextMenuItems.Add(new ContextMenuItem {
            Name = "Search Recipes Using This Material",
            OnClick = () => {
                AgentRecipeProductList.Instance()->SearchForRecipesUsingItem(item.ItemId);
            }
        });

        ContextMenuHelper.Open(contextMenuItems);
    }
}

// async can't live in unsafe so we define an extension method.
internal static class DutyLootPreviewAddonExtensions {
    internal static async Task LoadDutyItemsAsync(this DutyLootPreviewAddon addon, uint contentId, CancellationToken token) {
        try {
            var loadTask = Task.Run(() => DutyLootItem.ForContent(contentId).ToList(), token);

            // Show loading message only if loading takes longer than 50ms
            if (await Task.WhenAny(loadTask, Task.Delay(50, token)) != loadTask) {
                token.ThrowIfCancellationRequested();
                addon.SetLoading();
            }

            token.ThrowIfCancellationRequested();
            addon.SetItems(await loadTask);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) {
            Services.PluginLog.Error(ex, "Failed to load duty loot");
        }
    }
}
