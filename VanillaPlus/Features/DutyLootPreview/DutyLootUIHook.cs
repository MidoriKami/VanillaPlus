using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using KamiToolKit.Classes;
using KamiToolKit.Classes.Controllers;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// Displays the "Open Duty Loot Table" button in Duty Finder
/// </summary>
internal unsafe class DutyLootUiHook {
    private AddonController<AddonJournalDetail>? journalDetail;
    private AddonController<AddonContentsFinder>? contentsFinder;

    private TextureButtonNode? lootButtonNode;

    public Action? OnShowDutyLootPreviewButtonClicked { get; init; }
    public Action<uint?>? DutyChanged { get; init; }

    private uint? lastSeenContentId;

    public void OnEnable() {
        journalDetail = new AddonController<AddonJournalDetail>("JournalDetail");
        journalDetail.OnAttach += AttachNodes;
        journalDetail.OnDetach += DetachNodes;
        journalDetail.OnRefresh += RefreshNodes;
        journalDetail.Enable();

        contentsFinder = new AddonController<AddonContentsFinder>("ContentsFinder");
        contentsFinder.OnUpdate += OnContentsFinderUpdate;
        contentsFinder.OnDetach += OnContentsFinderDetach;
        contentsFinder.Enable();
    }

    public void OnDisable() {
        journalDetail?.Dispose();
        journalDetail = null;

        contentsFinder?.Dispose();
        contentsFinder = null;
    }

    private void AttachNodes(AddonJournalDetail* addon) {
        var dutyTitleNode = addon->GetNodeById(37);
        if (dutyTitleNode is null) return;

        Services.PluginLog.Info("Got Root Container");

        var existing = addon->DutyNameTextNode; // ID: 38
        if (existing is null) return;

        Services.PluginLog.Info($"Got DutyName Text Node");

        lootButtonNode = new TextureButtonNode {
            TexturePath = "ui/uld/Inventory.tex",
            TextureCoordinates = new Vector2(90.0f, 125.0f),
            TextureSize = new Vector2(32.0f, 32.0f),

            Position = new Vector2(420.0f, 68.0f),
            Size = new Vector2(32.0f, 32.0f),
            TooltipString = "View Loot that can be earned in this duty.",
            OnClick = OnLootButtonClicked,
        };
        lootButtonNode.AttachNode(dutyTitleNode, NodePosition.AfterTarget);
    }

    private void RefreshNodes(AddonJournalDetail* addon) {
        if (lootButtonNode != null) { lootButtonNode.IsVisible = false; }
        // We only want to show the button if we're attached to the duty finder,
        // other addons need not apply.
        var addonContentsFinder = Services.GameGui.GetAddonByName<AddonContentsFinder>("ContentsFinder");
        if (addonContentsFinder == null) { return; }

        Services.PluginLog.Info($"addon->ParentId: {addon->ParentId}, acf->Id: {addonContentsFinder->Id}");
        if (addon->ParentId != addonContentsFinder->Id) { return; }

        if (lootButtonNode != null) { lootButtonNode.IsVisible = true; }
    }

    private void DetachNodes(AddonJournalDetail* addon) {
        lootButtonNode?.Dispose();
        lootButtonNode = null;
    }

    private void OnContentsFinderUpdate(AddonContentsFinder* addon) {
        if (addon == null) { return; }

        var agentContentsFinder = AgentContentsFinder.Instance();
        if (agentContentsFinder == null) { return; }
        if (!agentContentsFinder->IsAgentActive()) { return; }

        var content = agentContentsFinder->SelectedDuty;
        if (content.Id == lastSeenContentId) { return; }

        if (content.ContentType == ContentsId.ContentsType.Regular) {
            lastSeenContentId = content.Id;
            DutyChanged?.Invoke(content.Id);
        } else {
            lastSeenContentId = null;
            DutyChanged?.Invoke(null);
        }
    }

    private void OnContentsFinderDetach(AddonContentsFinder* addon) {
        if (addon == null) { return; }

        lastSeenContentId = null;
        DutyChanged?.Invoke(null);
    }

    private void OnLootButtonClicked() {
        OnShowDutyLootPreviewButtonClicked?.Invoke();
    }
}
