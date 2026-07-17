using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using VanillaPlus.Features.DutyLootPreview.Data;
using VanillaPlus.Features.DutyLootPreview.Nodes;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// Displays the "Open Duty Loot Table" button in Duty Finder
/// </summary>
public class DutyLootJournalUiController : IDisposable {
    private AddonController<AddonJournalDetail>? journalDetail;
    private DutyLootOpenWindowButtonNode? lootButtonNode;
    private ushort attachedAddonId;

    public required DutyLootDataLoader DataLoader;

    public Action? OnButtonClicked { get; init; }

    public void Enable() {
        unsafe {
            journalDetail = new AddonController<AddonJournalDetail> {
                AddonName = "JournalDetail",
                OnSetup = SetupJournalDetail,
                OnFinalize = FinalizeJournalDetail,
                OnRefresh = RefreshJournalDetail,
            };
        }

        journalDetail.Enable();

        DataLoader.OnChanged += OnDataChanged;
    }

    public void Dispose() {
        DataLoader.OnChanged -= OnDataChanged;

        journalDetail?.Dispose();
        journalDetail = null;
    }

    private unsafe void SetupJournalDetail(AddonJournalDetail* addon) {
        var dutyTitleNode = addon->GetNodeById(37);
        if (dutyTitleNode is null) return;

        var existing = addon->DutyNameTextNode; // ID: 38
        if (existing is null) return;

        // Only attach if parent is the Duty Finder
        if (addon->ParentId is not 0) {
            var parentAddon = RaptureAtkUnitManager.Instance()->GetAddonById(addon->ParentId);
            if (parentAddon is null || (parentAddon->NameString != "ContentsFinder" && parentAddon->NameString != "RaidFinder")) {
                return;
            }
        }

        if (journalDetail is not null) {
            CleanupAttached();
        }

        lootButtonNode = new DutyLootOpenWindowButtonNode(DataLoader) {
            Position = new Vector2(420.0f, 68.0f),
            Size = new Vector2(32.0f, 32.0f),
            TextTooltip = Strings.DutyLoot_Tooltip_JournalButton,
            OnClick = () => OnButtonClicked?.Invoke(),
            IsVisible = ShouldShow(),
        };
        lootButtonNode.AttachNode(dutyTitleNode, NodePosition.AfterTarget);
        attachedAddonId = addon->Id;
    }

    private unsafe void RefreshJournalDetail(AddonJournalDetail* addon)
        => lootButtonNode?.IsVisible = ShouldShow();

    private unsafe void OnDataChanged() {
        if (lootButtonNode == null) return;

        var addon = Service<IGameGui>.Get().GetAddonByName<AddonJournalDetail>("JournalDetail");
        if (addon == null) return;

        lootButtonNode.IsVisible = ShouldShow();
    }

    private bool ShouldShow() {
        var lootData = DataLoader.ActiveDutyLootData;
        return lootData is not null || DataLoader.IsLoading;
    }

    private unsafe void FinalizeJournalDetail(AddonJournalDetail* addon) {
        if (addon->Id != attachedAddonId) return;

        CleanupAttached();
    }

    private void CleanupAttached() {
        lootButtonNode?.Dispose();
        lootButtonNode = null;
        attachedAddonId = 0;
    }
}
