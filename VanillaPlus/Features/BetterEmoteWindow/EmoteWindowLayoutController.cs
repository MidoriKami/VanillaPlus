using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Extensions;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.BetterEmoteWindow;

public class EmoteWindowLayoutController : IAsyncDisposable {
    private AddonController? emoteController;
    private readonly Dictionary<uint, EmoteCategoryLayoutState> originalCategoryLayouts = [];
    private EmoteListLayoutState? originalMainListLayout;
    private float hiddenCategoryDescriptionHeight;
    private bool categoryDescriptionWasVisible;
    private nint layoutAddonAddress;

    public async Task EnableAsync() {
        unsafe {
            emoteController = new AddonController {
                AddonName = "Emote",
                OnSetup = ApplyLayout,
                OnFinalize = FinalizeEmote,
            };
        }

        await emoteController.EnableAsync();
    }

    public async ValueTask DisposeAsync() {
        await emoteController.DisposeAsyncSafe();
        emoteController = null;
        ClearLayoutSnapshot();
    }

    private unsafe void ApplyLayout(AtkUnitBase* addon) {
        RestoreAppliedLayout(addon);

        var descriptionNode = CaptureLayoutSnapshot(addon);
        if (descriptionNode is null) return;

        hiddenCategoryDescriptionHeight = Math.Max(0.0f, descriptionNode->Height - 8.0f);
        descriptionNode->ToggleVisibility(false);

        foreach (var nodeId in new[] { 4u, 16u, 21u, 41u }) MoveCategoryNode(addon, nodeId);
        ResizeMainList(addon);
        addon->Size = addon->Size;
    }

    private unsafe void FinalizeEmote(AtkUnitBase* addon) => RestoreAppliedLayout(addon);

    private unsafe void MoveCategoryNode(AtkUnitBase* addon, uint nodeId) {
        var node = addon->GetNodeById(nodeId);
        if (node is null || !originalCategoryLayouts.TryGetValue(nodeId, out var layout)) return;

        node->Position = layout.Position - new Vector2(0.0f, hiddenCategoryDescriptionHeight);
        node->Size = layout.Size;
        if (nodeId is 4) node->Size += new Vector2(0.0f, hiddenCategoryDescriptionHeight);
    }

    private unsafe void ResizeMainList(AtkUnitBase* addon) {
        if (originalMainListLayout is not { } layout) return;

        var list = addon->GetComponentListById(4);
        if (list is null) return;

        var targetListHeight = (ushort)Math.Max(0, (int)Math.Round(layout.ListHeight + hiddenCategoryDescriptionHeight));
        list->SetSize((ushort)Math.Max(0, (int)layout.ListWidth), targetListHeight);
        list->ListHeight = (short)targetListHeight;
    }

    private unsafe AtkResNode* CaptureLayoutSnapshot(AtkUnitBase* addon) {
        var descriptionNode = addon->GetNodeById(11);
        if (descriptionNode is null) {
            IPluginLog.Get().Warning("[BetterEmoteWindow] Unable to find Emote category description node #11.");
            return null;
        }

        foreach (var nodeId in new[] { 4u, 16u, 21u, 41u }) {
            var node = addon->GetNodeById(nodeId);
            if (node is not null) originalCategoryLayouts[nodeId] = new EmoteCategoryLayoutState(node->Position, node->Size);
        }

        var list = addon->GetComponentListById(4);
        if (list is not null) {
            originalMainListLayout = new EmoteListLayoutState(
                list->ListWidth,
                list->ListHeight);
        }

        categoryDescriptionWasVisible = descriptionNode->IsVisible();
        layoutAddonAddress = (nint)addon;
        return descriptionNode;
    }

    private unsafe void RestoreAppliedLayout(AtkUnitBase* addon) {
        if (layoutAddonAddress is 0) return;

        if (layoutAddonAddress != (nint)addon) {
            ClearLayoutSnapshot();
            return;
        }

        var list = addon->GetComponentListById(4);
        if (list is not null && originalMainListLayout is { } listLayout) {
            list->SetSize(
                (ushort)Math.Max(0, (int)listLayout.ListWidth),
                (ushort)Math.Max(0, (int)listLayout.ListHeight));
            list->ListHeight = listLayout.ListHeight;
        }

        foreach (var (nodeId, layout) in originalCategoryLayouts) {
            var node = addon->GetNodeById(nodeId);
            if (node is null) continue;

            node->Position = layout.Position;
            node->Size = layout.Size;
        }

        var descriptionNode = addon->GetNodeById(11);
        if (descriptionNode is not null) descriptionNode->ToggleVisibility(categoryDescriptionWasVisible);
        addon->Size = addon->Size;
        ClearLayoutSnapshot();
    }

    private void ClearLayoutSnapshot() {
        originalCategoryLayouts.Clear();
        originalMainListLayout = null;
        hiddenCategoryDescriptionHeight = 0.0f;
        categoryDescriptionWasVisible = false;
        layoutAddonAddress = 0;
    }
}
