using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Classes.ContextMenu;
using KamiToolKit.Classes.Controllers;
using KamiToolKit.Nodes;
using ContextMenu = KamiToolKit.Classes.ContextMenu.ContextMenu;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// The window that shows loot for a duty.
/// </summary>
public unsafe class DutyLootPreviewAddon : NativeAddon {
    private static string NoItemsMessage => Strings("DutyLoot_NoItemsMessage");
    private static string NoResultsMessage => Strings("DutyLoot_NoResultsMessage");
    private static string LoadingMessage => Strings("DutyLoot_LoadingMessage");

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

    public required DutyLootPreviewConfig Config { get; init; }
    
    private ContextMenu? contextMenu;

    public override void Dispose() {
        base.Dispose();

        loadingCts?.Cancel();
        loadingCts?.Dispose();
        loadingCts = null;

        contentsFinder?.Dispose();
        contentsFinder = null;
    }

    protected override void OnSetup(AtkUnitBase* addon) {
        const float filterBarHeight = 36f;
        const float separatorHeight = 4f;

        contextMenu = new ContextMenu();
        Services.ClientState.TerritoryChanged += OnTerritoryChanged;

        filterBarNode = new DutyLootFilterBarNode {
            Position = ContentStartPosition,
            Size = new Vector2(ContentSize.X, filterBarHeight),
            OnFilterChanged = _ => updateRequested = true,
        };
        filterBarNode.AttachNode(this);

        separatorNode = new HorizontalLineNode {
            Position = ContentStartPosition + new Vector2(0, filterBarHeight),
            Size = new Vector2(ContentSize.X, 4.0f),
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
            Position = new Vector2(ContentStartPosition.X + ContentSize.X * 0.2f / 2.0f, listAreaPosition.Y + 15),
            Size = new Vector2(ContentSize.X * 0.8f, ContentSize.Y * 3.0f / 4.0f),
            TextColor = ColorHelper.GetColor(1),
            LineSpacing = 18,
            TextFlags = TextFlags.MultiLine | TextFlags.Edge | TextFlags.WordWrap,
            AlignmentType = AlignmentType.Center,
            String = NoItemsMessage,
        };
        noItemsTextNode.AttachNode(this);

        contentsFinder = new AddonController<AddonContentsFinder>("ContentsFinder");
        contentsFinder.OnAttach += OnContentsFinderUpdate;
        contentsFinder.OnRefresh += OnContentsFinderUpdate;
        contentsFinder.OnDetach += _ => Close();
        contentsFinder.Enable();

        UpdateList(true);

        LoadCurrentDuty(); // We might already be in a duty
    }
    
    protected override void OnUpdate(AtkUnitBase* addon) => UpdateList();
    
    protected override void OnFinalize(AtkUnitBase* addon) {
        contextMenu?.Dispose();
        Services.ClientState.TerritoryChanged -= OnTerritoryChanged;
    }

    private static void OnDutyLootItemLeftClick(DutyLootItem item) {
        if (item.CanTryOn) {
            AgentTryon.TryOn(0, item.ItemId);
        }
    }

    private void OnDutyLootItemRightClick(DutyLootItem item) {
        if (contextMenu is null) return;
        contextMenu.Clear();

        if (item.CanTryOn) {
            contextMenu.AddItem(Strings("DutyLoot_Context_TryOn"), () => AgentTryon.TryOn(0, item.ItemId));
        }

        var isFavorite = Config.FavoriteItems.Contains(item.ItemId);
        contextMenu.AddItem(new ContextMenuItem {
            Name = isFavorite
                ? Strings("DutyLoot_Context_RemoveFavorite")
                : Strings("DutyLoot_Context_AddFavorite"),
            OnClick = () => {
                if (isFavorite) {
                    Config.FavoriteItems.Remove(item.ItemId);
                } else {
                    Config.FavoriteItems.Add(item.ItemId);
                }
                Config.Save();
                UpdateFavoriteStars();
            },
        });

        contextMenu.AddItem(Strings("DutyLoot_Context_SearchItem"), () => ItemFinderModule.Instance()->SearchForItem(item.ItemId));
        contextMenu.AddItem(Strings("DutyLoot_Context_Link"), () => AgentChatLog.Instance()->LinkItem(item.ItemId));
        contextMenu.AddItem(Strings("DutyLoot_Context_SearchRecipes"), () => AgentRecipeProductList.Instance()->SearchForRecipesUsingItem(item.ItemId));

        contextMenu.Open();
    }

    internal void SetItems(IEnumerable<DutyLootItem> itemsEnumerable) {
        items = itemsEnumerable.ToList();
        isLoading = false;
        updateRequested = true;
    }

    internal void SetLoading() {
        items = [];
        isLoading = true;
        updateRequested = true;
    }

    private void LoadDuty(uint? contentId) {
        if (contentId == lastLoadedContentId) return;
        lastLoadedContentId = contentId;

        if (contentId is null) {
            items = [];
            isLoading = false;
            updateRequested = true;
            lastLoadedContentId = null;
            return;
        }

        loadingCts?.Cancel();
        loadingCts?.Dispose();
        loadingCts = null;

        loadingCts = new CancellationTokenSource();
        var token = loadingCts.Token;
        var id = contentId.Value;

        Task.Run(() => this.LoadDutyItemsAsync(id, token), token);
    }

    private void OnContentsFinderUpdate(AddonContentsFinder* addon) {
        if (!IsOpen) return;

        var content = AgentContentsFinder.Instance()->SelectedDuty;
        if (content.ContentType == ContentsId.ContentsType.Regular) {
            LoadDuty(content.Id);
        }
    }

    private void OnTerritoryChanged(ushort territory) {
        LoadCurrentDuty();
    }

    private void LoadCurrentDuty() {
        var contentFinderId = GameMain.Instance()->CurrentContentFinderConditionId;
        if (contentFinderId == 0) { return; }

        LoadDuty(contentFinderId);
    }

    private void UpdateList(bool isOpening = false) {
        if (scrollingAreaNode is null || noItemsTextNode is null || filterBarNode is null) return;
        if (!updateRequested && !isOpening) return;
        updateRequested = false;

        var filteredItems = filterBarNode.CurrentFilter switch {
            LootFilter.Favorites => items.Where(item => Config.FavoriteItems.Contains(item.ItemId)),
            LootFilter.Equipment => items.Where(item => item.ItemSortCategory is 5 or 56),
            LootFilter.Misc => items.Where(item => item.ItemSortCategory is not (5 or 56)),
            _ => items,
        };

        var list = scrollingAreaNode.ContentNode;
        var listUpdated = list.SyncWithListData(
            filteredItems,
            node => node.Item,
            data => new DutyLootNode {
                Size = new Vector2(list.Width, 36.0f),
                Item = data,
                IsFavorite = Config.FavoriteItems.Contains(data.ItemId),
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
            noItemsTextNode.String = isLoading ? LoadingMessage : hasData ? NoResultsMessage : NoItemsMessage;
        }
    }

    private void UpdateFavoriteStars() {
        if (scrollingAreaNode is null) return;

        foreach (var node in scrollingAreaNode.ContentNode.GetNodes<DutyLootNode>()) {
            node.IsFavorite = Config.FavoriteItems.Contains(node.Item.ItemId);
        }
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
