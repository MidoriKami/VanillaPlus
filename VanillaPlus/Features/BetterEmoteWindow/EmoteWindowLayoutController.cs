using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.BetterEmoteWindow;

public class EmoteWindowLayoutController : IAsyncDisposable {
    private AddonController? emoteController;
    private readonly Dictionary<uint, (Vector2 Position, Vector2 Size)> originalCategoryLayouts = [];
    private EmoteListLayoutState? originalMainListLayout;
    private float hiddenCategoryDescriptionHeight;
    private bool categoryDescriptionWasVisible;
    private nint layoutAddonAddress;
    private short nativeMainListRows;

    public async Task EnableAsync() {
        unsafe {
            emoteController = new AddonController {
                AddonName = "Emote",
                OnSetup = ApplyLayout,
                OnFinalize = FinalizeEmote,
            };
        }

        IAddonLifecycle.Get().RegisterListener(AddonEvent.PreSetup, "Emote", OnEmoteSetupPhase);
        await emoteController.EnableAsync();
    }

    public async ValueTask DisposeAsync() {
        IAddonLifecycle.Get().UnregisterListener(OnEmoteSetupPhase);

        unsafe {
            var addon = (AtkUnitBase*)RaptureAtkUnitManager.Instance()->GetAddonByName("Emote");
            if (addon is not null) {
                RestoreAppliedLayout(addon);
                ApplyMainListRowCount(addon, expanded: false);
            }
            else ClearLayoutSnapshot();
        }

        await emoteController.DisposeAsyncSafe();
        emoteController = null;
        ClearLayoutSnapshot();
    }

    private unsafe void OnEmoteSetupPhase(AddonEvent type, AddonArgs args) {
        if (type is not AddonEvent.PreSetup) return;

        nativeMainListRows = 0;

        var addon = (AtkUnitBase*)args.Addon.Address;
        var list = addon->GetComponentListById(4);
        if (list is null || list->NumVisibleRows <= 0) {
            IPluginLog.Get().Warning("[BetterEmoteWindow] Emote list #4 is not available during PreSetup.");
            return;
        }

        nativeMainListRows = list->NumVisibleRows;
        list->SetVisibleRowCount((short)(nativeMainListRows + 3));
    }

    private unsafe void ApplyLayout(AtkUnitBase* addon) {
        RestoreAppliedLayout(addon);
        ApplyMainListRowCount(addon, expanded: true);

        var descriptionNode = CaptureLayoutSnapshot(addon);
        if (descriptionNode is null) return;

        hiddenCategoryDescriptionHeight = Math.Max(0.0f, descriptionNode->Height - 8.0f);
        descriptionNode->ToggleVisibility(false);

        foreach (var nodeId in new[] { 4u, 16u, 21u, 41u }) MoveCategoryNode(addon, nodeId);
        ResizeMainList(addon);
        addon->UpdateCollisionNodeList(false);
    }

    private unsafe void ApplyMainListRowCount(AtkUnitBase* addon, bool expanded) {
        if (nativeMainListRows <= 0) return;

        var list = addon->GetComponentListById(4);
        var rowCount = expanded
            ? (short)(nativeMainListRows + 3)
            : nativeMainListRows;
        if (list is null || list->NumVisibleRows == rowCount) return;

        list->SetVisibleRowCount(rowCount);
    }

    private unsafe void FinalizeEmote(AtkUnitBase* _) {
        ClearLayoutSnapshot();
        nativeMainListRows = 0;
    }

    private unsafe void MoveCategoryNode(AtkUnitBase* addon, uint nodeId) {
        var node = addon->GetNodeById(nodeId);
        if (node is null || !originalCategoryLayouts.TryGetValue(nodeId, out var layout)) return;

        node->Position = layout.Position - new Vector2(0.0f, hiddenCategoryDescriptionHeight);
        node->Size = nodeId is 4
            ? layout.Size + new Vector2(0.0f, hiddenCategoryDescriptionHeight)
            : layout.Size;
    }

    private unsafe void ResizeMainList(AtkUnitBase* addon) {
        if (originalMainListLayout is not { } layout) return;

        var list = addon->GetComponentListById(4);
        if (list is null) return;

        if (list->CollisionNode is not null && layout.CollisionSize != Vector2.Zero) {
            ((AtkResNode*)list->CollisionNode)->Size = layout.CollisionSize + new Vector2(0.0f, hiddenCategoryDescriptionHeight);
        }

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
            if (node is not null) originalCategoryLayouts[nodeId] = (node->Position, node->Size);
        }

        var list = addon->GetComponentListById(4);
        if (list is not null) {
            var collisionNode = (AtkResNode*)list->CollisionNode;
            originalMainListLayout = new EmoteListLayoutState(
                list->ListWidth,
                list->ListHeight,
                collisionNode is null ? Vector2.Zero : collisionNode->Size);
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
            var collisionNode = (AtkResNode*)list->CollisionNode;
            if (collisionNode is not null && listLayout.CollisionSize != Vector2.Zero) {
                collisionNode->Size = listLayout.CollisionSize;
            }

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
        addon->UpdateCollisionNodeList(false);
        ClearLayoutSnapshot();
    }

    private void ClearLayoutSnapshot() {
        originalCategoryLayouts.Clear();
        originalMainListLayout = null;
        hiddenCategoryDescriptionHeight = 0.0f;
        categoryDescriptionWasVisible = false;
        layoutAddonAddress = 0;
    }

    private readonly record struct EmoteListLayoutState(
        short ListWidth,
        short ListHeight,
        Vector2 CollisionSize
    );
}
