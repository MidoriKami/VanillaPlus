using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using VanillaPlus.Features.DutyLootPreview.Data;
using VanillaPlus.Features.DutyLootPreview.Nodes;
using Action = System.Action;

namespace VanillaPlus.Features.DutyLootPreview;

/// <summary>
/// Displays the "Open Duty Loot Table" button in Duty Finder
/// </summary>
public unsafe class DutyLootJournalUiController {
    private AddonController<AddonJournalDetail>? journalDetail;
    private DutyLootOpenWindowButtonNode? lootButtonNode;

    public required DutyLootDataLoader DataLoader;

    public Action? OnButtonClicked { get; init; }

    public void OnEnable() {
        journalDetail = new AddonController<AddonJournalDetail>("JournalDetail");
        journalDetail.OnAttach += AttachNodes;
        journalDetail.OnDetach += DetachNodes;
        journalDetail.OnRefresh += RefreshNodes;
        journalDetail.Enable();

        DataLoader.OnChanged += OnDataChanged;
    }

    public void OnDisable() {
        DataLoader.OnChanged -= OnDataChanged;

        journalDetail?.Dispose();
        journalDetail = null;
    }

    private void AttachNodes(AddonJournalDetail* addon) {
        var dutyTitleNode = addon->GetNodeById(37);
        if (dutyTitleNode is null) return;

        var existing = addon->DutyNameTextNode; // ID: 38
        if (existing is null) return;

        lootButtonNode = new DutyLootOpenWindowButtonNode(DataLoader) {
            Position = new Vector2(420.0f, 68.0f),
            Size = new Vector2(32.0f, 32.0f),
            TextTooltip = Strings.DutyLoot_Tooltip_JournalButton,
            OnClick = () => OnButtonClicked?.Invoke(),
            IsVisible = ShouldShow(addon),
        };
        lootButtonNode.AttachNode(dutyTitleNode, NodePosition.AfterTarget);
    }

    private void RefreshNodes(AddonJournalDetail* addon)
        => lootButtonNode?.IsVisible = ShouldShow(addon);

    private void OnDataChanged() {
        if (lootButtonNode == null) return;

        var addon = Services.GameGui.GetAddonByName<AddonJournalDetail>("JournalDetail");
        if (addon == null) return;

        lootButtonNode.IsVisible = ShouldShow(addon);
    }

    private bool ShouldShow(AddonJournalDetail* addon) {
        // Only show if parent is the Duty Finder
        if (addon->ParentId is not 0) {
            var parentAddon = RaptureAtkUnitManager.Instance()->GetAddonById(addon->ParentId);
            if (parentAddon is null || parentAddon->NameString != "ContentsFinder") {
                return false;
            }
        }

        var lootData = DataLoader.ActiveDutyLootData;
        return lootData is not null || DataLoader.IsLoading;
    }

    private void DetachNodes(AddonJournalDetail* addon) {
        lootButtonNode?.Dispose();
        lootButtonNode = null;
    }
}
