using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
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
            Size = new Vector2(250.0f, 350.0f),
        };
    }

    private const string NoItemsMessage = "No loot data found for this duty.\n\nData is provided by a third party and may be incomplete.";
    private const string LoadingMessage = "Loading loot data...";

    private ScrollingAreaNode<VerticalListNode>? scrollingAreaNode;
    private TextNode? noItemsTextNode;

    private bool updateRequested = true;
    private bool isLoading;
    private List<DutyLootItem> items = [];

    private AddonController<AddonContentsFinder>? contentsFinder;
    private CancellationTokenSource? loadingCts;
    private uint? lastLoadedContentId;

    internal void SetItems(IEnumerable<DutyLootItem> itemsEnumerable) {
        items = itemsEnumerable.ToList();
        isLoading = false;
        updateRequested = true;
    }

    internal void Clear() {
        items = [];
        isLoading = false;
        updateRequested = true;
    }

    internal void SetLoading() {
        items = [];
        isLoading = true;
        updateRequested = true;
    }

    private uint? GetCurrentContentId() {
        var agent = AgentContentsFinder.Instance();
        if (agent == null || !agent->IsAgentActive()) return null;

        var content = agent->SelectedDuty;
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
        scrollingAreaNode = new ScrollingAreaNode<VerticalListNode> {
            Position = ContentStartPosition,
            Size = ContentSize,
            ContentHeight = 100,
        };
        scrollingAreaNode.ContentNode.FitContents = true;
        scrollingAreaNode.AttachNode(this);

        noItemsTextNode = new TextNode {
            Position = ContentStartPosition,
            Size = ContentSize,
            TextColor = ColorHelper.GetColor(1),
            LineSpacing = 18,
            TextFlags = TextFlags.MultiLine | TextFlags.Edge | TextFlags.WordWrap,
            AlignmentType = AlignmentType.Center,
            SeString = NoItemsMessage
        };
        noItemsTextNode.AttachNode(this);

        contentsFinder = new AddonController<AddonContentsFinder>("ContentsFinder");
        contentsFinder.OnRefresh += OnContentsFinderUpdate;
        contentsFinder.Enable();

        LoadCurrentDuty();
        UpdateList(true);
    }

    public override void Dispose() {
        loadingCts?.Cancel();
        loadingCts?.Dispose();
        loadingCts = null;

        contentsFinder?.Dispose();
        contentsFinder = null;

        base.Dispose();
    }

    protected override void OnUpdate(AtkUnitBase* addon) => UpdateList();

    private void UpdateList(bool isOpening = false) {
        if (scrollingAreaNode is null || noItemsTextNode is null) return;
        if (!updateRequested && !isOpening) return;
        updateRequested = false;

        var list = scrollingAreaNode.ContentNode;
        var listUpdated = list.SyncWithListData(
            items,
            node => node.Item,
            data => new DutyLootNode {
                Size = new Vector2(list.Width, 36.0f),
                Item = data,
            }
        );

        if (listUpdated) {
            scrollingAreaNode.ScrollPosition = 0;
            scrollingAreaNode.ContentHeight = scrollingAreaNode.ContentNode.Nodes.Sum(node => node.IsVisible ? node.Height : 0.0f);
        }

        if (list.GetNodes<DutyLootNode>().Count() > 0) {
            scrollingAreaNode.IsVisible = true;
            noItemsTextNode.IsVisible = false;
        } else {
            scrollingAreaNode.IsVisible = false;
            noItemsTextNode.IsVisible = true;
            noItemsTextNode.SeString = isLoading ? LoadingMessage : NoItemsMessage;
        }
    }
}

// async can't live in unsafe so we define an extension method.
internal static class DutyLootPreviewAddonExtensions {
    internal static async Task LoadDutyItemsAsync(this DutyLootPreviewAddon addon, uint contentId, CancellationToken token) {
        try {
            var loadTask = Task.Run(() => DutyLootItem.ForContent(contentId)
                .OrderBy(item => item.ItemSortCategory is 5 or 56 ? uint.MaxValue : item.ItemSortCategory)
                .ToList(), token);

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
