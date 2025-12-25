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
public unsafe class DutyLootJournalUiController {
    private AddonController<AddonJournalDetail>? journalDetail;
    private TextureButtonNode? lootButtonNode;

    public Action? OnButtonClicked { get; init; }

    public void OnEnable() {
        journalDetail = new AddonController<AddonJournalDetail>("JournalDetail");
        journalDetail.OnAttach += AttachNodes;
        journalDetail.OnDetach += DetachNodes;
        journalDetail.OnRefresh += RefreshNodes;
        journalDetail.Enable();
    }

    public void OnDisable() {
        journalDetail?.Dispose();
        journalDetail = null;
    }

    private void AttachNodes(AddonJournalDetail* addon) {
        var dutyTitleNode = addon->GetNodeById(37);
        if (dutyTitleNode is null) return;

        var existing = addon->DutyNameTextNode; // ID: 38
        if (existing is null) return;

        lootButtonNode = new TextureButtonNode {
            TexturePath = "ui/uld/Inventory.tex",
            TextureCoordinates = new Vector2(90.0f, 125.0f),
            TextureSize = new Vector2(32.0f, 32.0f),

            Position = new Vector2(420.0f, 68.0f),
            Size = new Vector2(32.0f, 32.0f),
            TooltipString = Strings("DutyLoot_Tooltip_JournalButton"),
            OnClick = () => OnButtonClicked?.Invoke(),
            IsVisible = ShouldShow,
        };
        lootButtonNode.AttachNode(dutyTitleNode, NodePosition.AfterTarget);
    }

    private void RefreshNodes(AddonJournalDetail* addon) {
        if (lootButtonNode == null) return;

        // We should only show the button if our parent is the duty finder.
        if (addon->ParentId is not 0) {
            var parentAddon = RaptureAtkUnitManager.Instance()->GetAddonById(addon->ParentId);
            if (parentAddon is null || parentAddon->NameString != "ContentsFinder") {
                lootButtonNode.IsVisible = false;
                return;
            }
        }

        lootButtonNode.IsVisible = ShouldShow;
    }

    private static bool ShouldShow => AgentContentsFinder.Instance()->SelectedDuty.ContentType == ContentsId.ContentsType.Regular;

    private void DetachNodes(AddonJournalDetail* addon) {
        lootButtonNode?.Dispose();
        lootButtonNode = null;
    }
}
