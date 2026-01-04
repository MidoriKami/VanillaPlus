using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.Gui;
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
    private static string NoItemsMessage => Strings.DutyLoot_NoItemsMessage;
    private static string NoResultsMessage => Strings.DutyLoot_NoResultsMessage;
    private static string LoadingMessage => Strings.DutyLoot_LoadingMessage;

    private DutyLootFilterBarNode? filterBarNode;
    private HorizontalLineNode? separatorNode;
    private ScrollingListNode? scrollingAreaNode;
    private TextNode? hintTextNode;

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
        Services.GameGui.AgentUpdate += OnAgentUpdate;

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

        scrollingAreaNode = new ScrollingListNode {
            Position = listAreaPosition,
            Size = listAreaSize,
            FitContents = true,
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
        Services.GameGui.AgentUpdate -= OnAgentUpdate;
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
            contextMenu.AddItem(
            Services.DataManager.GetAddonText(2426), // Try On
            () => AgentTryon.TryOn(0, item.ItemId));
        }

        var isFavorite = Config.FavoriteItems.Contains(item.ItemId);
        contextMenu.AddItem(new ContextMenuItem {
            Name = isFavorite
                ? Services.DataManager.GetAddonText(8324) // Remove from Favorites
                : Services.DataManager.GetAddonText(8323), // Add to Favorites
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

        contextMenu.AddItem(
            Services.DataManager.GetAddonText(4379), // Search for Item
            () => ItemFinderModule.Instance()->SearchForItem(item.ItemId));

        contextMenu.AddItem(
            Services.DataManager.GetAddonText(4697), // Link
            () => AgentChatLog.Instance()->LinkItem(item.ItemId));

        contextMenu.AddItem(
            Services.DataManager.GetAddonText(13439), // Search Recipes Using This Material
            () => AgentRecipeProductList.Instance()->SearchForRecipesUsingItem(item.ItemId));

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

    private void LoadDuty(uint? contentId, bool forceReload = false) {
        if (!forceReload && contentId == lastLoadedContentId) return;
        if (forceReload) contentId ??= lastLoadedContentId;
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

        if (content.ContentType == ContentsId.ContentsType.Roulette) {
            Close();
            return;
        }

        if (content.ContentType == ContentsId.ContentsType.Regular) {
            LoadDuty(content.Id);
        }
    }

    private void OnTerritoryChanged(ushort territory) {
        LoadCurrentDuty();
    }

    private void OnAgentUpdate(AgentUpdateFlag flag) {
        if (flag.HasFlag(AgentUpdateFlag.UnlocksUpdate)) {
            LoadDuty(null, true);
        }
    }

    private void LoadCurrentDuty() {
        var contentFinderId = GameMain.Instance()->CurrentContentFinderConditionId;
        if (contentFinderId == 0) { return; }

        LoadDuty(contentFinderId);
    }

    private void UpdateList(bool isOpening = false) {
        if (scrollingAreaNode is null || hintTextNode is null || filterBarNode is null) return;
        if (!updateRequested && !isOpening) return;
        updateRequested = false;

        var filteredItems = filterBarNode.CurrentFilter switch {
            LootFilter.Favorites => items.Where(item => Config.FavoriteItems.Contains(item.ItemId)),
            LootFilter.Equipment => items.Where(item => item.IsEquipment),
            LootFilter.Misc => items.Where(item => !item.IsEquipment),
            _ => items,
        };

        var listUpdated = scrollingAreaNode.SyncWithListData(
            filteredItems,
            node => node.Item,
            data => new DutyLootNode {
                Size = new Vector2(scrollingAreaNode.ContentWidth, 36.0f),
                Item = data,
                IsFavorite = Config.FavoriteItems.Contains(data.ItemId),
                OnLeftClick = OnDutyLootItemLeftClick,
                OnRightClick = OnDutyLootItemRightClick
            }
        );

        if (listUpdated) {
            scrollingAreaNode.ScrollPosition = 0;
            scrollingAreaNode.RecalculateLayout();

            scrollingAreaNode.ReorderNodes((a, b) => {
                if (a is not DutyLootNode left || b is not DutyLootNode right) return 0;
                return left.Item.CompareTo(right.Item);
            });
        }

        var hasData = items.Count > 0 && !isLoading;
        var hasResults = scrollingAreaNode.GetNodes<DutyLootNode>().Any();

        filterBarNode.IsVisible = hasData;
        separatorNode!.IsVisible = hasData;
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

    private void UpdateFavoriteStars() {
        if (scrollingAreaNode is null) return;

        foreach (var node in scrollingAreaNode.GetNodes<DutyLootNode>()) {
            node.IsFavorite = Config.FavoriteItems.Contains(node.Item.ItemId);
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
