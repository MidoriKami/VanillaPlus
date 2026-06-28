using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using Dalamud.Utility;
using Lumina.Text.ReadOnly;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Extensions;
using VanillaPlus.Features.QuickPanelAdjustments;
using VanillaPlus.Native.Addons;
using VanillaPlus.Utilities;
using HotbarSlotType = FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureHotbarModule.HotbarSlotType;

namespace VanillaPlus.Features.CommandPanelExpansion;

/// <summary>
/// EXPERIMENTAL: Expands the native Command Panel in two ways:
///
/// 1. Reveals the game's hidden page tabs. The QuickPanel addon ships with 10 page-tab radio
///    buttons (node ids 7..16) but only shows the first 4 by default.
///
/// 2. Enlarges the slot grid past the native 5x5. The 25 native slots (DragDrop component nodes
///    19..43) live under the parent Res node (id 17); extra plugin-drawn DragDrop nodes are appended
///    to that same parent to fill a larger grid, and the container / background / window frame grow
///    to fit. Actions, macros, items, emotes, commands, etc. dropped onto the extra slots are saved
///    per-page to the plugin's config, executed on click, and removed by dragging them off the panel.
///
/// WARNING: the game only stores data for the first 4 pages. Revealing extra pages can corrupt the
/// Command Panel save and crash the game if their (garbage) native slots are edited.
/// </summary>
public class CommandPanelExpansion : GameModification
{
    public override ModificationInfo ModificationInfo => new()
    {
        DisplayName = "Command Panel Expansion",
        Description = "Experimental: lets you change the number of Command Panel pages and expand its slot " +
                      "grid beyond the native 5x5 columns and rows.",
        Type = ModificationType.UserInterface,
        Authors = ["Richter"],
    };

    public override bool IsExperimental => true;

    private const uint SlotContainerNodeId = 17;
    private const uint PanelBackgroundNodeId = 44;
    private const uint WindowComponentNodeId = 45;
    private const uint RootNodeId = 1;
    private const uint HeaderCollisionNodeId = 11;

    // The page-tab radio buttons are sequential top-level nodes starting at id 7 (10 of them).
    private const uint FirstPageTabNodeId = 7;
    private const int NativePageTabCount = 10;
    private const int MinimumPageCount = 4;
    private const int MaximumPageCount = 9;

    // Native 5x5 DragDrop component nodes (ids 19..43). Pages 5+ use 0-based index 4+.
    private const uint FirstNativeSlotNodeId = 19;
    private const int FirstExtendedPageIndex = 4;

    private const float SlotPitch = 46.0f;
    private const float SlotSize = 44.0f;

    private const int NativeColumns = 5;
    private const int NativeRows = 5;
    private const int NativeSlotCount = NativeColumns * NativeRows;
    private const int MaximumGridSize = 10;

    private const float BaseWindowWidth = 292.0f;
    private const float BaseWindowHeight = 320.0f;
    private const float HeaderHeight = 40.0f;

    private const int MacroSharedSetOffset = 256;
    private const uint InvalidActionType = 0xFFFFFFFF;

    private const string ConfigTitle = "Command Panel Expansion Settings";
    private const string CategoryPages = "Pages";
    private const string CategorySize = "Grid Size";
    private const uint NativePagesWarningIconId = 60074u;
    private const string NativePagesWarningTooltip =
        "When enabled, pages 5-9 use the game's native slot storage instead of the plugin. " +
        "FFXIV developers added these pages but hid them - revealing them lets the game save your layout " +
        "(for example via Settings Migration).\n\n" +
        "Warning: these pages may contain corrupted skills from unused memory. " +
        "Editing or removing them can crash the game.\n\n" +
        "Recommended: disabled";

    // Window-frame / collision nodes (inside the window component) that should stretch with the panel.
    private static readonly uint[] WindowFrameNodeIds = [12, 10, 9, 8];

    // Hotbar item ids carry +1,000,000 when the item is high quality.
    private const uint HighQualityItemOffset = 1_000_000;

    private AddonController? quickPanelController;
    private CommandPanelExpansionConfig? config;

    // Cached copy of the sibling QuickPanel Adjustments config, read so the custom slots can honour its
    // "Hide the empty slots in the command panel" option. Reloaded when the panel reopens.
    private QuickPanelAdjustmentsConfig? quickPanelAdjustmentsConfig;
    private ConfigAddon? configAddon;
    private readonly List<ExtraSlot> extraSlots = [];

    // The QuickPanel is hidden (not finalized) when closed, so our nodes survive but the game
    // corrupts their visuals on re-show. We track visibility and rebuild the slots on re-open.
    private bool wasVisible;
    private bool forceRebuild;
    // The dimensions of the node grid that physically exists (built at the full size only while the
    // settings window is open, otherwise at exactly the configured size).
    private int builtGridColumns;
    private int builtGridRows;
    // Whether the built grid omitted the native 5x5 area (those cells are permanently covered by the
    // game's own slots, so building plugin slots there is pure waste - see NativeAreaSlotsNeeded).
    private bool builtSkipNativeArea;
    // The configured size last applied to the panel/window and slot visibility.
    private int builtColumns;
    private int builtRows;
    private int currentPage = -1;
    private ExtraSlot? dragSourceSlot;
    private bool suppressSourceDiscard;
    private int nextSlotReferenceIndex = 1;

    private nint quickPanelAddonAddress;
    private bool nativeSlotHooksAttached;
    private readonly NativeSlotHook[] nativeSlotHooks = new NativeSlotHook[NativeSlotCount];

    private CrossDragSourceKind crossDragSourceKind;
    private int crossDragSourceNativeIndex = -1;
    private HotbarSlotType crossDragSourceNativeType;
    private uint crossDragSourceNativeId;

    private readonly HotbarSlotType[] nativeDragSnapshotTypes = new HotbarSlotType[NativeSlotCount];
    private readonly uint[] nativeDragSnapshotIds = new uint[NativeSlotCount];

    // Native→custom move: the game may restore the source slot on DragDropEnd after we already cleared it.
    private int pendingNativeSlotClearIndex = -1;

    private enum CrossDragSourceKind
    {
        None,
        Plugin,
        Native,
    }

    private sealed class NativeSlotHook
    {
        public required int SlotIndex;
        public CustomEventListener? Listener;
    }

    private sealed class ExtraSlot
    {
        public required DragDropNode Node;
        public required int Column;
        public required int Row;
        public required int ReferenceIndex;
        public HotbarSlotType CommandType;
        public uint CommandId;

        // The quantity/cost overlay last pushed to this slot's nodes. RefreshSlotVisuals runs every frame,
        // so the overlay text is only re-written (an allocating, unsafe operation) when it actually changes.
        // null means the overlay is currently cleared.
        public string? AppliedQuantityText;
        public bool AppliedQuantityIsCount;
    }

    public override async Task OnEnableAsync()
    {
        config = await CommandPanelExpansionConfig.Load();
        await RefreshExternalConfigsAsync();

        configAddon = new ConfigAddon
        {
            Size = new Vector2(450.0f, 220.0f),
            InternalName = "CommandPanelExpansionConfig",
            Title = ConfigTitle,
            Config = config,
        };

        configAddon.AddCategory(CategoryPages)
            .AddIntSlider("Number of Pages", MinimumPageCount, MaximumPageCount, nameof(config.PageCount))
            .AddCheckboxWithWarningIcon(
                "Use Native Elements for Pages 5+",
                nameof(config.UseNativeElementsForPages5Plus),
                NativePagesWarningIconId,
                NativePagesWarningTooltip);

        configAddon.AddCategory(CategorySize)
            .AddIntSlider("Columns", NativeColumns, MaximumGridSize, nameof(config.Columns))
            .AddIntSlider("Rows", NativeRows, MaximumGridSize, nameof(config.Rows));

        OpenConfigAction = configAddon.Toggle;

        unsafe
        {
            quickPanelController = new AddonController
            {
                AddonName = "QuickPanel",
                OnSetup = OnQuickPanelSetup,
                OnUpdate = UpdateQuickPanel,
                OnRefresh = UpdateQuickPanel,
                OnFinalize = OnQuickPanelFinalize,
            };
        }

        await Services.Framework.RunSafely(quickPanelController.Enable);
    }

    public override async Task OnDisableAsync()
    {
        await Services.Framework.RunSafely(() =>
        {
            DisposeExtraSlots();
            quickPanelController?.Dispose();
        });
        quickPanelController = null;

        await Task.WhenAll(configAddon?.DisposeAsync().AsTask() ?? Task.CompletedTask);
        configAddon = null;

        config = null;
        quickPanelAdjustmentsConfig = null;
    }

    private unsafe void OnQuickPanelSetup(AtkUnitBase* addon)
    {
        // Fresh setup: any nodes from a previous lifetime are gone, rebuild from scratch.
        forceRebuild = true;
        wasVisible = false;
        RevealPageTabs(addon);
    }

    private unsafe void OnQuickPanelFinalize(AtkUnitBase* addon)
    {
        DisposeExtraSlots();
        wasVisible = false;
        forceRebuild = true;
    }

    private void DisposeExtraSlots()
    {
        DetachNativeSlotHooks();

        foreach (var slot in extraSlots)
        {
            slot.Node.Dispose();
        }

        extraSlots.Clear();
        builtGridColumns = 0;
        builtGridRows = 0;
        builtSkipNativeArea = false;
        builtColumns = 0;
        builtRows = 0;
        currentPage = -1;
        quickPanelAddonAddress = 0;
        ResetDragState();
        nextSlotReferenceIndex = 1;
    }

    private unsafe void UpdateQuickPanel(AtkUnitBase* addon)
    {
        if (config is null) return;

        RevealPageTabs(addon);

        // Re-show without a fresh setup leaves our nodes corrupted; rebuild them on the
        // hidden -> visible transition (the same effect as changing the grid size by hand).
        var isVisible = addon->IsVisible;
        if (isVisible && !wasVisible)
        {
            forceRebuild = true;

            // Pick up any change to QuickPanel Adjustments' "Hide the empty slots" option made while
            // the panel was closed, so the custom slots stay in step with the native ones.
            _ = RefreshExternalConfigsAsync();
        }
        wasVisible = isVisible;

        if (!isVisible)
        {
            DetachNativeSlotHooks();
            return;
        }

        quickPanelAddonAddress = (nint)addon;
        EnsureNativeSlotHooks(addon);

        var columns = Math.Clamp(config.Columns, NativeColumns, MaximumGridSize);
        var rows = Math.Clamp(config.Rows, NativeRows, MaximumGridSize);

        var container = addon->GetNodeById(SlotContainerNodeId);
        if (container is null) return;

        // Build only the slots that can actually appear. While the settings window is open the grid is
        // built at full size with every cell, so dragging the Columns/Rows sliders (or toggling the page
        // / native-element options) only changes visibility - no per-frame node churn. While it is
        // closed the grid is built at exactly the configured size, and the native 5x5 area is skipped
        // entirely when no page ever hides the native slots. A panel with no visible modifications
        // therefore builds ZERO extra nodes, so opening/closing it costs nothing. Switching open/closed
        // rebuilds only once, on the deliberate open/close of the settings window.
        var configOpen = configAddon?.IsOpen ?? false;
        var buildColumns = configOpen ? MaximumGridSize : columns;
        var buildRows = configOpen ? MaximumGridSize : rows;
        var skipNativeArea = !configOpen && !NativeAreaSlotsNeeded();

        if (forceRebuild ||
            buildColumns != builtGridColumns ||
            buildRows != builtGridRows ||
            skipNativeArea != builtSkipNativeArea)
        {
            RebuildExtraSlots(addon, container, buildColumns, buildRows, skipNativeArea);

            builtGridColumns = buildColumns;
            builtGridRows = buildRows;
            builtSkipNativeArea = skipNativeArea;

            forceRebuild = false;
            currentPage = -1;

            // Force the resize/visibility pass below to run against the freshly built grid.
            builtColumns = 0;
            builtRows = 0;
        }

        // Resize the panel/window only when the configured grid size actually changes - re-applying
        // identical sizes every frame is wasted work.
        if (columns != builtColumns || rows != builtRows)
        {
            ResizePanel(addon, columns, rows);

            builtColumns = columns;
            builtRows = rows;
        }

        var page = GetCurrentPage(addon);
        if (page != currentPage)
        {
            currentPage = page;
            LoadPageIntoSlots(page);
        }

        ApplyNativeSlotVisibility(addon, page);
        ApplyPluginSlotVisibility(page);
        ApplyDragLock();
        RefreshDynamicSlotVisuals();
    }

    private static bool IsNativeGridCell(int column, int row)
        => column < NativeColumns && row < NativeRows;

    private bool ShouldShowNativeSlots(int page)
        => page < FirstExtendedPageIndex || (config?.UseNativeElementsForPages5Plus ?? false);

    // The plugin only needs to draw slots over the native 5x5 area when some configured page hides the
    // native slots - i.e. extended pages (5+) exist and their native elements are disabled. Otherwise
    // those cells are always covered by the game's own slots, so building plugin slots there is wasted
    // work (and the source of the open/close lag when the panel has no visible modifications).
    private bool NativeAreaSlotsNeeded()
    {
        if (config is null) return false;

        var pageCount = Math.Clamp(config.PageCount, MinimumPageCount, MaximumPageCount);
        return pageCount > FirstExtendedPageIndex && !config.UseNativeElementsForPages5Plus;
    }

    private unsafe void ApplyNativeSlotVisibility(AtkUnitBase* addon, int page)
    {
        var showNative = ShouldShowNativeSlots(page);

        for (var index = 0; index < NativeSlotCount; index++)
        {
            var slotNode = addon->GetNodeById(FirstNativeSlotNodeId + (uint)index);
            if (slotNode is null) continue;

            slotNode->ToggleVisibility(showNative);
        }
    }

    // Honour QuickPanel Adjustments' "Hide the empty slots in the command panel" option on the custom
    // slots, but only while that modification is actually enabled - so the custom squares appear and
    // disappear together with the native ones.
    private bool ShouldHideEmptySlots
        => quickPanelAdjustmentsConfig?.HideEmptySlots == true
           && System.SystemConfig.EnabledModifications.Contains(nameof(CommandPanelAdjustments));

    // The "Hide empty slots" preference lives in the sibling QuickPanel Adjustments modification; reload
    // its config when the panel reopens so toggling that option is reflected on the custom slots. Only
    // read it while that modification is enabled, so we don't create its config file for users who never
    // turned it on.
    private async Task RefreshExternalConfigsAsync()
    {
        try
        {
            quickPanelAdjustmentsConfig =
                System.SystemConfig.EnabledModifications.Contains(nameof(CommandPanelAdjustments))
                    ? await QuickPanelAdjustmentsConfig.Load()
                    : null;
        }
        catch (Exception exception)
        {
            Services.PluginLog.Exception(exception);
        }
    }

    private void ApplyPluginSlotVisibility(int page)
    {
        var showNative = ShouldShowNativeSlots(page);
        var hideEmpty = ShouldHideEmptySlots;

        // The grid is always built at its maximum size; only the slots within the configured
        // columns/rows are shown (the rest of the pre-built grid stays hidden).
        var columns = builtColumns;
        var rows = builtRows;

        foreach (var slot in extraSlots)
        {
            var inGrid = slot.Column < columns && slot.Row < rows;
            var visible = inGrid && (!IsNativeGridCell(slot.Column, slot.Row) || !showNative);
            slot.Node.IsVisible = visible;

            // Drop the empty-slot frame when the option is on so an empty custom square is fully hidden,
            // matching the native slots. Filled slots are unaffected (the icon covers the frame anyway).
            var empty = !IsValidCommand(slot.CommandType, slot.CommandId);
            slot.Node.DragDropBackgroundNode.IsVisible = visible && !(empty && hideEmpty);
        }
    }

    private unsafe void RevealPageTabs(AtkUnitBase* addon)
    {
        if (config is null) return;

        var pageCount = Math.Clamp(config.PageCount, MinimumPageCount, MaximumPageCount);

        for (var index = 0; index < NativePageTabCount; index++)
        {
            var tabNode = addon->GetNodeById(FirstPageTabNodeId + (uint)index);
            if (tabNode is null) continue;

            tabNode->ToggleVisibility(index < pageCount);
        }
    }

    private unsafe int GetCurrentPage(AtkUnitBase* addon)
    {
        for (var index = 0; index < NativePageTabCount; index++)
        {
            var radio = addon->GetComponentById<AtkComponentRadioButton>(FirstPageTabNodeId + (uint)index);
            if (radio is not null && radio->IsSelected)
            {
                return index;
            }
        }

        return currentPage < 0 ? 0 : currentPage;
    }

    // Builds a slot grid of the given size. Slots outside the configured columns/rows are hidden by
    // ApplyPluginSlotVisibility rather than destroyed, so resizing within the built grid never recreates
    // nodes. The grid is sized to the full maximum while the settings window is open (so the sliders are
    // smooth) and to the configured size otherwise (so opening the panel is cheap). When
    // <paramref name="skipNativeArea"/> is set the native 5x5 cells are omitted entirely - they are
    // permanently covered by the game's slots, so a panel with no visible modifications builds nothing.
    private unsafe void RebuildExtraSlots(AtkUnitBase* addon, AtkResNode* container, int columns, int rows, bool skipNativeArea)
    {
        DisposeExtraSlots();

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                if (skipNativeArea && IsNativeGridCell(column, row)) continue;

                var node = new DragDropNode
                {
                    Size = new Vector2(SlotSize, SlotSize),
                    Position = new Vector2(column * SlotPitch, row * SlotPitch),
                    IsVisible = true,
                    IsClickable = true,
                };

                node.IconId = 0;

                CommandPanelSlotPresenter.ApplyNativeSlotFrame(node);

                var slot = new ExtraSlot
                {
                    Node = node,
                    Column = column,
                    Row = row,
                    ReferenceIndex = nextSlotReferenceIndex++,
                };

                node.OnBegin = _ => {
                    crossDragSourceKind = CrossDragSourceKind.Plugin;
                    crossDragSourceNativeIndex = -1;
                    dragSourceSlot = slot;
                    suppressSourceDiscard = false;
                    SnapshotNativeSlots();
                };
                node.OnEnd = _ => OnPluginDragEnd(slot);
                node.OnRollOver = _ => OnSlotRollOver(slot);
                node.OnRollOut = _ => OnSlotRollOut(slot);
                node.OnPayloadAccepted = (_, payload) => OnSlotPayloadAccepted(slot, payload);
                node.OnClicked = _ => OnSlotClicked(slot);
                node.OnDiscard = _ => OnSlotDiscarded(slot);

                node.AttachNode(container);
                extraSlots.Add(slot);
            }
        }
    }

    // True when the sibling Command Panel Sync modification is enabled. Its registry key is the
    // modification's class name (GameModification.Name); referencing the type keeps this rename-safe.
    private static bool IsCommandPanelSyncActive
        => System.SystemConfig.EnabledModifications.Contains(nameof(CommandPanelSync.CommandPanelSync));

    private static ulong CurrentContentId
        => Services.ClientState.IsLoggedIn ? Services.PlayerState.ContentId : 0;

    private static List<SavedSlotCommand> CloneSlots(List<SavedSlotCommand> source)
    {
        var copy = new List<SavedSlotCommand>(source.Count);
        foreach (var entry in source)
        {
            copy.Add(new SavedSlotCommand
            {
                Page = entry.Page,
                Column = entry.Column,
                Row = entry.Row,
                CommandType = entry.CommandType,
                CommandId = entry.CommandId,
            });
        }

        return copy;
    }

    // The per-character slot list, migrating the legacy global list into the first character that
    // asks for it. Returns null when no character is available or (with createIfMissing false) the
    // character has no stored slots yet.
    private List<SavedSlotCommand>? GetCharacterSlots(bool createIfMissing)
    {
        if (config is null) return null;

        var contentId = CurrentContentId;
        if (contentId == 0) return null;

        if (config.CharacterSlots.TryGetValue(contentId, out var existing))
        {
            return existing;
        }

        // One-time migration: hand the old single global layout to the first character that opens
        // the panel so existing users keep it; later characters start with their own empty layout.
        if (config.SavedSlots.Count > 0)
        {
            var migrated = CloneSlots(config.SavedSlots);
            config.SavedSlots = [];
            config.CharacterSlots[contentId] = migrated;
            _ = config.Save();
            return migrated;
        }

        if (!createIfMissing) return null;

        var fresh = new List<SavedSlotCommand>();
        config.CharacterSlots[contentId] = fresh;
        return fresh;
    }

    // The slot list reads/writes should target. With Command Panel Sync active every character shares
    // one layout (SharedSlots, seeded from the current character the first time); otherwise each
    // character uses its own list.
    private List<SavedSlotCommand>? GetActiveSlots(bool createIfMissing)
    {
        if (config is null) return null;

        if (!IsCommandPanelSyncActive)
        {
            return GetCharacterSlots(createIfMissing);
        }

        // First use of sync with nothing shared yet: the current character establishes the shared
        // layout ("overwrite the other characters with the current one").
        if (config.SharedSlots.Count == 0)
        {
            var own = GetCharacterSlots(createIfMissing: false);
            if (own is { Count: > 0 })
            {
                config.SharedSlots = CloneSlots(own);
            }
        }

        return config.SharedSlots;
    }

    // After mutating the active list while sync is on, copy it onto the shared layout and every
    // per-character entry (including the current one) so all characters are overwritten with the
    // current character's slots, and keep that layout if sync is later disabled.
    private void MirrorActiveSlotsToAllCharacters(List<SavedSlotCommand> activeSlots)
    {
        if (config is null || !IsCommandPanelSyncActive) return;

        config.SharedSlots = CloneSlots(activeSlots);

        // Make sure the current character is among the entries the loop overwrites, even if it had no
        // stored slots yet (e.g. sync was enabled before this character ever saved anything).
        var currentId = CurrentContentId;
        if (currentId != 0 && !config.CharacterSlots.ContainsKey(currentId))
        {
            config.CharacterSlots[currentId] = [];
        }

        foreach (var key in config.CharacterSlots.Keys.ToList())
        {
            config.CharacterSlots[key] = CloneSlots(activeSlots);
        }
    }

    private void LoadPageIntoSlots(int page)
    {
        if (config is null) return;

        var slots = GetActiveSlots(createIfMissing: false);

        // Index this page's saved commands by grid cell once, instead of an O(slots * savedSlots)
        // linear scan (with a per-slot closure allocation) for every slot.
        var pageSlots = new Dictionary<(int Column, int Row), SavedSlotCommand>();
        if (slots is not null)
        {
            foreach (var entry in slots)
            {
                if (entry.Page == page)
                {
                    pageSlots[(entry.Column, entry.Row)] = entry;
                }
            }
        }

        foreach (var slot in extraSlots)
        {
            if (pageSlots.TryGetValue((slot.Column, slot.Row), out var saved))
            {
                ApplyCommandToSlot(slot, (HotbarSlotType)saved.CommandType, saved.CommandId);
            }
            else
            {
                ApplyCommandToSlot(slot, HotbarSlotType.Empty, 0);
            }
        }
    }

    private void ApplyCommandToSlot(ExtraSlot slot, HotbarSlotType type, uint id)
    {
        slot.CommandType = type;
        slot.CommandId = id;

        if (IsValidCommand(type, id))
        {
            slot.Node.IconId = GetCommandIcon(type, id);
            slot.Node.Payload = BuildPayload(type, id);
            slot.Node.Payload.ReferenceIndex = (short)slot.ReferenceIndex;
        }
        else
        {
            slot.Node.IconId = 0;
            slot.Node.Payload = new DragDropPayload();
        }

        RefreshSlotVisuals(slot);
    }

    private void RefreshDynamicSlotVisuals()
    {
        foreach (var slot in extraSlots)
        {
            if (!slot.Node.IsVisible || !IsValidCommand(slot.CommandType, slot.CommandId)) continue;

            try
            {
                RefreshSlotVisuals(slot);
            }
            catch (Exception exception)
            {
                Services.PluginLog.Exception(exception);
            }
        }
    }

    private unsafe void RefreshSlotVisuals(ExtraSlot slot)
    {
        if (!IsValidCommand(slot.CommandType, slot.CommandId))
        {
            CommandPanelSlotPresenter.Clear(slot.Node, ref slot.AppliedQuantityText);
            return;
        }

        var module = RaptureHotbarModule.Instance();
        if (module is null) return;

        var scratch = &module->ScratchSlot;
        scratch->Set(slot.CommandType, slot.CommandId);
        scratch->LoadIconId();
        scratch->LoadCostDataForSlot();

        var state = ComputeSlotState(slot, scratch);
        CommandPanelSlotPresenter.Apply(slot.Node, in state, ref slot.AppliedQuantityText, ref slot.AppliedQuantityIsCount);
    }

    // Resolves how a filled slot should look by reading the live hotbar / action / inventory state into a
    // presentation-only ActionSlotVisualState. All FFXIVClientStructs hotbar access lives here; the
    // CommandPanelSlotPresenter then renders the node purely from the returned snapshot.
    private unsafe ActionSlotVisualState ComputeSlotState(ExtraSlot slot, RaptureHotbarModule.HotbarSlot* scratch)
    {
        var supportsVisualUpdates = SupportsActionVisualUpdates(scratch);

        // The range/LoS indicator only applies to actions (and their PvP variants). Items, gearsets,
        // mounts, emotes, etc. report a valid ActionType, so IsSlotActionTargetInRange2 would return
        // "out of range" for them (they have no range concept) and falsely paint the red frame + "×".
        var actionType = scratch->GetActionTypeForSlotType(scratch->ApparentSlotType);
        var rangeRelevant = actionType is ActionType.Action or ActionType.PvPAction;
        var outOfRange = rangeRelevant &&
                         supportsVisualUpdates &&
                         !scratch->IsSlotActionTargetInRange2(scratch->ApparentSlotType, scratch->ApparentActionId);

        // The cooldown must be read from ActionManager: GetSlotActionCooldownPercentage on the shared
        // ScratchSlot never reports a live cooldown (it is not a tracked hotbar slot).
        var hasCooldown = false;
        var cooldownProgress = 0.0f;
        if (supportsVisualUpdates)
        {
            var apparentActionId = scratch->ApparentActionId;
            var actionManager = ActionManager.Instance();
            if (actionManager is not null && actionManager->IsRecastTimerActive(actionType, apparentActionId))
            {
                var total = actionManager->GetRecastTime(actionType, apparentActionId);
                if (total > 0.0f)
                {
                    var elapsed = actionManager->GetRecastTimeElapsed(actionType, apparentActionId);
                    cooldownProgress = Math.Clamp(elapsed / total, 0.0f, 1.0f);
                    hasCooldown = true;
                }
            }
        }

        var highlighted = supportsVisualUpdates &&
                          scratch->IsActionHighlighted(scratch->CommandType, scratch->CommandId);

        var (quantityText, quantityIsCount) = ComputeQuantityOverlay(slot, scratch);

        return new ActionSlotVisualState
        {
            IsMacro = slot.CommandType is HotbarSlotType.Macro,
            IsUnusable = !scratch->IsSlotUsable(scratch->CommandType, scratch->CommandId),
            OutOfRange = outOfRange,
            HasCooldown = hasCooldown,
            CooldownProgress = cooldownProgress,
            Highlighted = highlighted,
            QuantityText = quantityText,
            QuantityIsCount = quantityIsCount,
        };
    }

    // Computes the bottom-right overlay text (and whether it is a count vs a cost value) for a filled slot.
    private static unsafe (string? Text, bool IsCount) ComputeQuantityOverlay(ExtraSlot slot, RaptureHotbarModule.HotbarSlot* scratch)
    {
        switch (scratch->ApparentSlotType)
        {
            case HotbarSlotType.Item:
            {
                // Stackable items show their inventory count bottom-right - including "x 0" when none are
                // held - while native leaves non-stackable items (max stack 1) blank. The shared ScratchSlot
                // does not reliably load item cost data, so read the stack size and live count directly.
                var rawId = scratch->ApparentActionId;
                var isHighQuality = rawId > HighQualityItemOffset;
                var itemId = rawId % HighQualityItemOffset;

                if (IsStackableItem(itemId))
                {
                    var inventory = InventoryManager.Instance();
                    var count = inventory is not null ? inventory->GetInventoryItemCount(itemId, isHighQuality) : 0;
                    return ($"x {count}", true);
                }

                return (null, false);
            }

            case HotbarSlotType.GearSet:
                // Gear sets show their 1-based number bottom-right (gear set ids are 0-based).
                return ((slot.CommandId + 1).ToString(), true);

            default:
                return (scratch->CostDisplayMode switch
                {
                    2 or 4 => ReadHotbarCostText(scratch),
                    1 or 3 when scratch->CostValue > 0 => scratch->CostValue.ToString(),
                    _ => null,
                }, false);
        }
    }

    private static unsafe bool SupportsActionVisualUpdates(RaptureHotbarModule.HotbarSlot* scratch)
    {
        var apparentType = scratch->ApparentSlotType;
        if (apparentType is HotbarSlotType.Empty) return false;

        return (uint)scratch->GetActionTypeForSlotType(apparentType) != InvalidActionType;
    }

    // Item stackability never changes at runtime, so cache the sheet lookup - ApplyQuantityOverlay runs
    // every frame for each filled item slot.
    private static readonly Dictionary<uint, bool> stackableItemCache = [];

    private static bool IsStackableItem(uint itemId)
    {
        if (!stackableItemCache.TryGetValue(itemId, out var stackable))
        {
            stackable = Services.DataManager.GetItem(itemId).StackSize > 1;
            stackableItemCache[itemId] = stackable;
        }

        return stackable;
    }

    private static unsafe string ReadHotbarCostText(RaptureHotbarModule.HotbarSlot* scratch)
    {
        scratch->GetCostTextForSlot(scratch->ApparentSlotType, scratch->ApparentActionId);

        fixed (byte* costTextPtr = scratch->CostText)
        {
            if (costTextPtr[0] is 0) return string.Empty;

            return new ReadOnlySeStringSpan(new ReadOnlySpan<byte>(costTextPtr, scratch->CostText.Length)).ExtractText();
        }
    }

    private void OnPluginDragEnd(ExtraSlot slot)
    {
        try
        {
            var trackedType = slot.CommandType;
            var trackedId = slot.CommandId;

            if (suppressSourceDiscard)
            {
                ResetDragState();
                return;
            }

            if (dragSourceSlot == slot && IsValidCommand(trackedType, trackedId))
            {
                TryClearPluginSlotAfterNativeDrop(slot, trackedType, trackedId);
            }

            ResetDragState();
        }
        catch (Exception exception)
        {
            Services.PluginLog.Exception(exception);
        }
    }

    private void TryClearPluginSlotAfterNativeDrop(ExtraSlot slot, HotbarSlotType type, uint id)
    {
        if (DidNativeSlotsAcceptPluginDrag(type, id))
        {
            ClearSlot(slot);
            return;
        }

        Services.Framework.RunOnFrameworkThread(() =>
        {
            if (!IsValidCommand(slot.CommandType, slot.CommandId)) return;
            if (slot.CommandType != type || slot.CommandId != id) return;
            if (!DidNativeSlotsAcceptPluginDrag(type, id)) return;

            ClearSlot(slot);
        });
    }

    private void SnapshotNativeSlots()
    {
        for (var index = 0; index < NativeSlotCount; index++)
        {
            if (TryGetNativeSlotCommandFromModule(currentPage, index, out var type, out var id))
            {
                nativeDragSnapshotTypes[index] = type;
                nativeDragSnapshotIds[index] = id;
            }
            else
            {
                nativeDragSnapshotTypes[index] = HotbarSlotType.Empty;
                nativeDragSnapshotIds[index] = 0;
            }
        }
    }

    private bool DidNativeSlotsAcceptPluginDrag(HotbarSlotType sourceType, uint sourceId)
    {
        for (var index = 0; index < NativeSlotCount; index++)
        {
            if (!TryGetNativeSlotCommandFromModule(currentPage, index, out var type, out var id)) continue;
            if (type != sourceType || id != sourceId) continue;

            if (nativeDragSnapshotTypes[index] != sourceType || nativeDragSnapshotIds[index] != sourceId)
            {
                return true;
            }
        }

        return false;
    }

    private ExtraSlot? FindSlotByReferenceIndex(short referenceIndex)
    {
        if (referenceIndex is 0) return null;

        foreach (var slot in extraSlots)
        {
            if (slot.ReferenceIndex == referenceIndex)
            {
                return slot;
            }
        }

        return null;
    }

    private ExtraSlot? FindSlotByReferenceIndex(short referenceIndex, ExtraSlot exclude)
    {
        if (referenceIndex is 0) return null;

        foreach (var slot in extraSlots)
        {
            if (slot.ReferenceIndex == referenceIndex && slot != exclude)
            {
                return slot;
            }
        }

        return null;
    }

    private ExtraSlot? FindSlotByPayload(DragDropPayload payload)
    {
        foreach (var slot in extraSlots)
        {
            if (!IsValidCommand(slot.CommandType, slot.CommandId)) continue;

            var built = BuildPayload(slot.CommandType, slot.CommandId);
            if (PayloadsMatch(built, payload))
            {
                return slot;
            }
        }

        return null;
    }

    private static bool PayloadsMatch(DragDropPayload left, DragDropPayload right)
        => left.Type == right.Type && left.Int1 == right.Int1 && left.Int2 == right.Int2;

    private ExtraSlot? ResolveInternalDragSource(ExtraSlot target, DragDropPayload payload, HotbarSlotType type, uint id)
    {
        if (dragSourceSlot is not null && dragSourceSlot != target)
        {
            return dragSourceSlot;
        }

        var byReference = FindSlotByReferenceIndex(payload.ReferenceIndex, target);
        if (byReference is not null &&
            byReference.CommandType == type &&
            byReference.CommandId == id)
        {
            return byReference;
        }

        return null;
    }

    private void ResetDragState()
    {
        dragSourceSlot = null;
        suppressSourceDiscard = false;
        crossDragSourceKind = CrossDragSourceKind.None;
        crossDragSourceNativeIndex = -1;
        crossDragSourceNativeType = HotbarSlotType.Empty;
        crossDragSourceNativeId = 0;
    }

    private unsafe void EnsureNativeSlotHooks(AtkUnitBase* addon)
    {
        if (nativeSlotHooksAttached) return;

        for (var index = 0; index < NativeSlotCount; index++)
        {
            var slotNode = addon->GetNodeById(FirstNativeSlotNodeId + (uint)index);
            if (slotNode is null) continue;

            var slotIndex = index;
            var listener = new CustomEventListener((_, eventType, _, atkEvent, atkEventData) =>
                OnNativeSlotEvent(addon, slotIndex, eventType, atkEvent, atkEventData));

            var eventTarget = (AtkEventTarget*)slotNode;

            slotNode->AtkEventManager.RegisterEvent(
                AtkEventType.DragDropBegin, 0, slotNode, eventTarget, listener, false);
            slotNode->AtkEventManager.RegisterEvent(
                AtkEventType.DragDropInsert, 0, slotNode, eventTarget, listener, false);
            slotNode->AtkEventManager.RegisterEvent(
                AtkEventType.DragDropEnd, 0, slotNode, eventTarget, listener, false);

            nativeSlotHooks[index] = new NativeSlotHook
            {
                SlotIndex = index,
                Listener = listener,
            };
        }

        nativeSlotHooksAttached = true;
    }

    private unsafe void DetachNativeSlotHooks()
    {
        if (!nativeSlotHooksAttached) return;

        var addon = (AtkUnitBase*)quickPanelAddonAddress;

        for (var index = 0; index < NativeSlotCount; index++)
        {
            var hook = nativeSlotHooks[index];
            if (hook?.Listener is null) continue;

            if (addon is not null)
            {
                var slotNode = addon->GetNodeById(FirstNativeSlotNodeId + (uint)index);
                if (slotNode is not null)
                {
                    slotNode->AtkEventManager.UnregisterEvent(AtkEventType.UnregisterAll, 0, hook.Listener, false);
                }
            }

            hook.Listener.Dispose();
            nativeSlotHooks[index] = null!;
        }

        nativeSlotHooksAttached = false;
    }

    private unsafe void OnNativeSlotEvent(
        AtkUnitBase* addon,
        int slotIndex,
        AtkEventType eventType,
        AtkEvent* atkEvent,
        AtkEventData* atkEventData)
    {
        if (!ShouldShowNativeSlots(currentPage)) return;

        try
        {
            switch (eventType)
            {
                case AtkEventType.DragDropBegin:
                    OnNativeDragBegin(addon, slotIndex);
                    break;

                case AtkEventType.DragDropInsert:
                    OnNativeDragInsert(addon, slotIndex, atkEvent, atkEventData);
                    break;

                case AtkEventType.DragDropEnd:
                    if (pendingNativeSlotClearIndex == slotIndex)
                    {
                        ClearNativeSlot((nint)addon, slotIndex);
                        ScheduleNativeSlotClear(slotIndex);
                    }

                    if (crossDragSourceKind is CrossDragSourceKind.Native &&
                        crossDragSourceNativeIndex == slotIndex)
                    {
                        ResetDragState();
                    }

                    break;
            }
        }
        catch (Exception exception)
        {
            Services.PluginLog.Exception(exception);
        }
    }

    private unsafe void OnNativeDragBegin(AtkUnitBase* addon, int slotIndex)
    {
        pendingNativeSlotClearIndex = -1;
        crossDragSourceKind = CrossDragSourceKind.Native;
        crossDragSourceNativeIndex = slotIndex;
        dragSourceSlot = null;
        suppressSourceDiscard = false;

        if (!TryGetNativeSlotCommand(addon, slotIndex, out crossDragSourceNativeType, out crossDragSourceNativeId))
        {
            crossDragSourceNativeType = HotbarSlotType.Empty;
            crossDragSourceNativeId = 0;
        }
    }

    private unsafe void OnNativeDragInsert(
        AtkUnitBase* addon,
        int slotIndex,
        AtkEvent* atkEvent,
        AtkEventData* atkEventData)
    {
        if (crossDragSourceKind is not CrossDragSourceKind.Plugin) return;

        var payload = DragDropPayload.FromDragDropInterface(atkEventData->DragDropData.DragDropInterface);
        var sourceSlot = dragSourceSlot
            ?? FindSlotByReferenceIndex(payload.ReferenceIndex)
            ?? FindSlotByPayload(payload);
        if (sourceSlot is null) return;
        if (!IsValidCommand(sourceSlot.CommandType, sourceSlot.CommandId)) return;

        atkEvent->SetEventIsHandled();
        atkEvent->State.StateFlags |= AtkEventStateFlags.HasReturnFlags;
        atkEvent->State.ReturnFlags = 1;

        suppressSourceDiscard = true;
        dragSourceSlot = sourceSlot;

        // A "swap" is only real when the target holds a DIFFERENT command. If the read-back equals the
        // command we are dragging, it is the in-flight payload leaking through (not the slot's prior
        // content), so this is a move into an empty slot - clear the source instead of copying it back.
        if (TryGetNativeSlotCommand(addon, slotIndex, out var targetType, out var targetId) &&
            IsValidCommand(targetType, targetId) &&
            !(targetType == sourceSlot.CommandType && targetId == sourceSlot.CommandId))
        {
            SetNativeSlot((nint)addon, slotIndex, sourceSlot.CommandType, sourceSlot.CommandId);
            ApplyCommandToSlot(sourceSlot, targetType, targetId);
            PersistSlot(sourceSlot);
        }
        else
        {
            SetNativeSlot((nint)addon, slotIndex, sourceSlot.CommandType, sourceSlot.CommandId);
            ClearSlot(sourceSlot);
        }

        ResetDragState();
    }

    private void HandleNativeToPluginDrop(ExtraSlot target)
    {
        if (crossDragSourceNativeIndex < 0) return;

        suppressSourceDiscard = true;

        if (IsValidCommand(target.CommandType, target.CommandId))
        {
            var targetType = target.CommandType;
            var targetId = target.CommandId;

            ApplyCommandToSlot(target, crossDragSourceNativeType, crossDragSourceNativeId);
            PersistSlot(target);

            SetNativeSlot(quickPanelAddonAddress, crossDragSourceNativeIndex, targetType, targetId);
        }
        else
        {
            ApplyCommandToSlot(target, crossDragSourceNativeType, crossDragSourceNativeId);
            PersistSlot(target);
            pendingNativeSlotClearIndex = crossDragSourceNativeIndex;
            ClearNativeSlot(quickPanelAddonAddress, crossDragSourceNativeIndex);
            ScheduleNativeSlotClear(crossDragSourceNativeIndex);
        }

        ResetDragState();
    }

    private void ClearNativeSlot(nint addonAddress, int slotIndex)
    {
        SetNativeSlot(addonAddress, slotIndex, HotbarSlotType.Empty, 0);
    }

    private void ScheduleNativeSlotClear(int slotIndex)
    {
        Services.Framework.RunOnFrameworkThread(() =>
        {
            if (pendingNativeSlotClearIndex != slotIndex) return;

            SetNativeSlot(quickPanelAddonAddress, slotIndex, HotbarSlotType.Empty, 0);
            pendingNativeSlotClearIndex = -1;
        });
    }

    private unsafe bool TryGetNativeSlotCommand(
        AtkUnitBase* addon,
        int slotIndex,
        out HotbarSlotType type,
        out uint id)
    {
        type = HotbarSlotType.Empty;
        id = 0;

        if (TryGetNativeSlotCommandFromModule(currentPage, slotIndex, out type, out id))
        {
            return true;
        }

        // Pages 0-3 are module-backed, so the module read above is authoritative - an empty slot is
        // genuinely empty. Reading the component's drag-drop interface mid-drag returns a stale payload
        // (a leftover/in-flight action), which would make an empty target look occupied and trigger a
        // bogus swap that leaves a random action in the source slot instead of clearing it.
        if (currentPage < FirstExtendedPageIndex) return false;

        var component = addon->GetComponentById<AtkComponentDragDrop>(FirstNativeSlotNodeId + (uint)slotIndex);
        if (component is null) return false;

        var payload = DragDropPayload.FromDragDropInterface(&component->AtkDragDropInterface);
        return ResolveCommand(payload, out type, out id);
    }

    private static unsafe bool TryGetNativeSlotCommandFromModule(int page, int slotIndex, out HotbarSlotType type, out uint id)
    {
        type = HotbarSlotType.Empty;
        id = 0;

        var module = QuickPanelModule.Instance();
        if (module is null || slotIndex < 0 || slotIndex >= NativeSlotCount) return false;

        switch (page)
        {
            case 0:
                type = module->Panel0CommandTypes[slotIndex];
                id = module->Panel0CommandIds[slotIndex];
                break;

            case 1:
                type = module->Panel1CommandTypes[slotIndex];
                id = module->Panel1CommandIds[slotIndex];
                break;

            case 2:
                type = module->Panel2CommandTypes[slotIndex];
                id = module->Panel2CommandIds[slotIndex];
                break;

            case 3:
                type = module->Panel3CommandTypes[slotIndex];
                id = module->Panel3CommandIds[slotIndex];
                break;

            default:
                return false;
        }

        return IsValidCommand(type, id);
    }

    private static unsafe void WriteNativeSlotModule(int page, int slotIndex, HotbarSlotType type, uint id)
    {
        var module = QuickPanelModule.Instance();
        if (module is null || slotIndex < 0 || slotIndex >= NativeSlotCount) return;

        switch (page)
        {
            case 0:
                module->Panel0CommandTypes[slotIndex] = type;
                module->Panel0CommandIds[slotIndex] = id;
                break;

            case 1:
                module->Panel1CommandTypes[slotIndex] = type;
                module->Panel1CommandIds[slotIndex] = id;
                break;

            case 2:
                module->Panel2CommandTypes[slotIndex] = type;
                module->Panel2CommandIds[slotIndex] = id;
                break;

            case 3:
                module->Panel3CommandTypes[slotIndex] = type;
                module->Panel3CommandIds[slotIndex] = id;
                break;
        }
    }

    private static unsafe void RefreshNativePanel(int page)
    {
        var agent = AgentQuickPanel.Instance();
        if (agent is null) return;

        agent->OpenPanel((uint)page, closeIfAlreadyOpen: false, showFirstTimeHelp: false);
    }

    private unsafe void SetNativeSlot(nint addonAddress, int slotIndex, HotbarSlotType type, uint id)
    {
        var addon = (AtkUnitBase*)addonAddress;
        if (addonAddress == 0) return;

        if (currentPage <= 3)
        {
            WriteNativeSlotModule(currentPage, slotIndex, type, id);
            ApplyNativeSlotVisual(addon, slotIndex, type, id);
            RefreshNativePanel(currentPage);
            return;
        }

        ApplyNativeSlotVisual(addon, slotIndex, type, id);
    }

    private static unsafe void ApplyNativeSlotVisual(AtkUnitBase* addon, int slotIndex, HotbarSlotType type, uint id)
    {
        var component = addon->GetComponentById<AtkComponentDragDrop>(FirstNativeSlotNodeId + (uint)slotIndex);
        if (component is null) return;

        var iconResNode = component->GetNodeById(2);
        if (iconResNode is not null && iconResNode->GetNodeType() is NodeType.Component)
        {
            var iconComponent = (AtkComponentIcon*)iconResNode->GetAsAtkComponentNode()->Component;

            if (IsValidCommand(type, id))
            {
                iconComponent->LoadIcon(GetCommandIcon(type, id));
                iconResNode->NodeFlags |= NodeFlags.Visible;
            }
            else
            {
                iconComponent->LoadIcon(0);
                iconResNode->NodeFlags &= ~NodeFlags.Visible;
            }
        }

        if (IsValidCommand(type, id))
        {
            BuildPayload(type, id).ToDragDropInterface(&component->AtkDragDropInterface);
        }
        else
        {
            component->AtkDragDropInterface.GetPayloadContainer()->Clear();
            component->AtkDragDropInterface.DragDropType = DragDropType.Nothing;
        }
    }

    private void OnSlotRollOver(ExtraSlot slot)
    {
        ShowSlotTooltip(slot);

        // Empty slots need the hover brighten driven manually (their component timeline only animates
        // while holding content).
        if (!IsValidCommand(slot.CommandType, slot.CommandId))
        {
            CommandPanelSlotPresenter.SetEmptySlotHovered(slot.Node, true);
        }
    }

    private void OnSlotRollOut(ExtraSlot slot)
    {
        slot.Node.HideTooltip();

        if (!IsValidCommand(slot.CommandType, slot.CommandId))
        {
            CommandPanelSlotPresenter.SetEmptySlotHovered(slot.Node, false);
        }
    }

    private static unsafe void ShowSlotTooltip(ExtraSlot slot)
    {
        if (!IsValidCommand(slot.CommandType, slot.CommandId)) return;

        var stage = AtkStage.Instance();
        if (stage is null || stage->DragDropManager.IsDragging) return;

        var module = RaptureHotbarModule.Instance();
        if (module is null) return;

        var scratch = &module->ScratchSlot;
        scratch->Set(slot.CommandType, slot.CommandId);
        scratch->LoadIconId();
        scratch->LoadCostDataForSlot();

        var apparentType = scratch->ApparentSlotType;
        var apparentId = scratch->ApparentActionId;

        AtkResNode* node = slot.Node;

        if (slot.CommandType is HotbarSlotType.Macro)
        {
            var macroTitle = scratch->GetDisplayNameForSlot(HotbarSlotType.Macro, slot.CommandId).ToString();
            node->ShowAnchoredTextTooltip(macroTitle);
            return;
        }

        var title = scratch->GetDisplayNameForSlot(apparentType, apparentId).ToString();

        if (UsesItemTooltip(slot.CommandType) || UsesItemTooltip(apparentType))
        {
            ShowItemTooltipWithTitle(node, slot.CommandId, scratch, slot.CommandType);
            return;
        }

        if (slot.CommandType is HotbarSlotType.GearSet)
        {
            node->ShowAnchoredTextTooltip(title);
            return;
        }

        // Real combat actions keep the action tooltip - it resolves upgraded/combo actions via the
        // apparent id and is the confirmed-correct path.
        if (slot.CommandType is HotbarSlotType.Action)
        {
            if ((uint)scratch->GetActionTypeForSlotType(apparentType) != InvalidActionType)
            {
                node->ShowActionTooltip(apparentId, title);
                return;
            }
        }
        else
        {
            // Non-combat command kinds (mounts, minions, fashion accessories, general/main/extra commands,
            // craft & buddy actions, ...) show their tooltip via their own DetailKind. These are standalone
            // entities with no "apparent" upgrade, and their apparent id is not always loaded on the scratch
            // slot (the icon comes from CommandId), so use the stored CommandId. The detail panel renders the
            // description (where the game supports it); the name/title comes from the Text arg.
            var detailKind = GetActionTooltipKind(slot.CommandType);
            if (detailKind is not null)
            {
                ShowActionDetailTooltip(node, scratch, slot.CommandType, slot.CommandId, detailKind.Value);
                return;
            }
        }

        if (!scratch->PopUpHelp.IsEmpty)
        {
            node->ShowAnchoredTextTooltip(scratch->PopUpHelp.ToString());
            return;
        }

        if (!string.IsNullOrEmpty(title))
        {
            node->ShowAnchoredTextTooltip(title);
        }
    }

    private static bool UsesItemTooltip(HotbarSlotType type)
        => type is HotbarSlotType.Item or HotbarSlotType.EventItem;

    // Maps a hotbar command type to the tooltip DetailKind the game uses for its "action help" panel, so
    // mounts/minions/commands/etc. show their own tooltip instead of being misread as a row in the Action
    // sheet. Returns null for types with no action-detail tooltip (they fall back to PopUpHelp/title text).
    private static DetailKind? GetActionTooltipKind(HotbarSlotType type) => type switch
    {
        HotbarSlotType.CraftAction => DetailKind.CraftingAction,
        HotbarSlotType.GeneralAction => DetailKind.GeneralAction,
        HotbarSlotType.MainCommand => DetailKind.MainCommand,
        HotbarSlotType.ExtraCommand => DetailKind.ExtraCommand,
        HotbarSlotType.Companion => DetailKind.Companion,
        HotbarSlotType.BuddyAction => DetailKind.BuddyAction,
        HotbarSlotType.PetAction => DetailKind.PetOrder,
        HotbarSlotType.Mount => DetailKind.Mount,
        HotbarSlotType.Ornament => DetailKind.Ornament,
        HotbarSlotType.ChocoboRaceAbility => DetailKind.ChocoboRaceAction,
        HotbarSlotType.ChocoboRaceItem => DetailKind.ChocoboRaceItem,
        HotbarSlotType.BgcArmyAction => DetailKind.BgcArmyAction,
        HotbarSlotType.PerformanceInstrument => DetailKind.Perform,
        HotbarSlotType.Glasses => DetailKind.Glasses,
        _ => null,
    };

    private static unsafe void ShowActionDetailTooltip(
        AtkResNode* node,
        RaptureHotbarModule.HotbarSlot* scratch,
        HotbarSlotType slotType,
        uint actionId,
        DetailKind kind)
    {
        var tooltipType = AtkTooltipType.Action;

        var tooltipArgs = stackalloc AtkTooltipManager.AtkTooltipArgs[1];
        tooltipArgs->Ctor();
        tooltipArgs->ActionArgs.Kind = kind;
        tooltipArgs->ActionArgs.Id = (int)actionId;
        tooltipArgs->ActionArgs.Flags = 0;

        // The Action detail panel only supplies the description; the name/title line comes from the Text
        // arg (the native action tooltip combines both), so add the display name as the title.
        var displayName = scratch->GetDisplayNameForSlot(slotType, actionId);
        if (displayName.Value is not null)
        {
            tooltipType |= AtkTooltipType.Text;
            tooltipArgs->TextArgs.AtkArrayType = 0;
            tooltipArgs->TextArgs.Text = displayName;
        }

        var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(node);
        if (addon is null) return;

        AtkStage.Instance()->TooltipManager.ShowTooltip(tooltipType, addon->Id, node, tooltipArgs);
    }

    // Player bag containers a usable item can live in. Used to anchor the item tooltip to the live
    // inventory slot so the Action Help window reports the real possessed count.
    private static readonly InventoryType[] PlayerBagInventories =
    [
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
    ];

    private static unsafe void ShowItemTooltipWithTitle(
        AtkResNode* node,
        uint itemId,
        RaptureHotbarModule.HotbarSlot* scratch,
        HotbarSlotType slotType)
    {
        var tooltipType = AtkTooltipType.Item;
        var tooltipArgs = stackalloc AtkTooltipManager.AtkTooltipArgs[1];
        tooltipArgs->Ctor();

        // Regular items: anchor the Action Help tooltip to the live inventory slot so the "quantity /
        // max stack" line reports the real held amount, exactly like hovering the item in the bag.
        // DetailKind.Item carries no inventory context, so its quantity falls back to a constant 1;
        // DetailKind.InventoryItem reads the real stack.
        var resolvedToInventory = false;
        if (slotType is HotbarSlotType.Item)
        {
            var highQuality = itemId > HighQualityItemOffset;
            var baseItemId = itemId % HighQualityItemOffset;

            if (TryGetItemInventoryLocation(baseItemId, highQuality, out var container, out var inventorySlot))
            {
                tooltipArgs->ItemArgs.Kind = DetailKind.InventoryItem;
                tooltipArgs->ItemArgs.InventoryType = container;
                tooltipArgs->ItemArgs.Slot = inventorySlot;
                tooltipArgs->ItemArgs.BuyQuantity = -1;
                tooltipArgs->ItemArgs.Flag1 = 0;
                resolvedToInventory = true;
            }
        }

        if (!resolvedToInventory)
        {
            // None held (count 0) or a key/event item: show the item by id. BuyQuantity feeds the
            // quantity line, so an item the player no longer holds is given 0 here rather than the
            // default 1 (key/event items are not stackable, so they keep the -1 "no quantity" marker).
            tooltipArgs->ItemArgs.Kind = DetailKind.Item;
            tooltipArgs->ItemArgs.ItemId = (int)itemId;
            tooltipArgs->ItemArgs.BuyQuantity = slotType is HotbarSlotType.Item ? 0 : -1;
            tooltipArgs->ItemArgs.Flag1 = 0;
        }

        // Item-detail tooltips in this flow do not render the name themselves, so add the hotbar display
        // name as the title line (this is what was lost when the count fix first dropped the Text arg).
        var displayName = scratch->GetDisplayNameForSlot(slotType, itemId);
        if (displayName.Value is not null)
        {
            tooltipType |= AtkTooltipType.Text;
            tooltipArgs->TextArgs.AtkArrayType = 0;
            tooltipArgs->TextArgs.Text = displayName;
        }

        var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(node);
        if (addon is null) return;

        AtkStage.Instance()->TooltipManager.ShowTooltip(tooltipType, addon->Id, node, tooltipArgs);
    }

    // Finds the first player-bag slot holding the given item (matching high-quality state), so the item
    // tooltip can be shown against the real inventory slot (DetailKind.InventoryItem) and report the live
    // possessed count. Returns false when none are held.
    private static unsafe bool TryGetItemInventoryLocation(uint itemId, bool highQuality, out InventoryType container, out short slot)
    {
        container = default;
        slot = 0;

        var inventory = InventoryManager.Instance();
        if (inventory is null) return false;

        foreach (var bag in PlayerBagInventories)
        {
            var bagContainer = inventory->GetInventoryContainer(bag);
            if (bagContainer is null || !bagContainer->IsLoaded) continue;

            for (var index = 0; index < bagContainer->Size; index++)
            {
                var item = bagContainer->GetInventorySlot(index);
                if (item is null || item->ItemId != itemId) continue;
                if (item->IsHighQuality() != highQuality) continue;

                container = bag;
                slot = item->Slot;
                return true;
            }
        }

        return false;
    }

    private void ApplyDragLock()
    {
        var locked = IsDraggingDisabled();

        foreach (var slot in extraSlots)
        {
            slot.Node.IsDraggable = !locked;
        }
    }

    private static unsafe bool IsDraggingDisabled()
    {
        var module = QuickPanelModule.Instance();
        return module is not null &&
               module->Settings.HasFlag(QuickPanelModule.QuickPanelSetting.DisableDraggingWithinCommandPanel);
    }

    // True when the dropped payload resolves to the same command as the tracked native cross-drag source.
    private bool NativeCrossDragMatchesPayload(DragDropPayload payload)
        => ResolveCommand(payload, out var type, out var id) &&
           type == crossDragSourceNativeType &&
           id == crossDragSourceNativeId;

    private void OnSlotPayloadAccepted(ExtraSlot slot, DragDropPayload payload)
    {
        try
        {
            // Only treat this as a native->plugin drop when the dropped payload actually matches the
            // recorded native source. This guards against a stale native cross-drag: if a native drag
            // ended without resetting our tracking and a different command is then dragged in from
            // elsewhere (e.g. the actions list), we must apply what was just dropped - not re-apply the
            // stale native snapshot. The fall-through path below resets the drag state.
            if (crossDragSourceKind is CrossDragSourceKind.Native &&
                crossDragSourceNativeIndex >= 0 &&
                NativeCrossDragMatchesPayload(payload))
            {
                HandleNativeToPluginDrop(slot);
                return;
            }

            if (!ResolveCommand(payload, out var type, out var id))
            {
                ClearSlot(slot);
                return;
            }

            var sourceSlot = ResolveInternalDragSource(slot, payload, type, id);

            if (sourceSlot is not null && sourceSlot != slot)
            {
                suppressSourceDiscard = true;

                if (IsValidCommand(slot.CommandType, slot.CommandId))
                {
                    var targetType = slot.CommandType;
                    var targetId = slot.CommandId;

                    ApplyCommandToSlot(slot, sourceSlot.CommandType, sourceSlot.CommandId);
                    PersistSlot(slot);

                    ApplyCommandToSlot(sourceSlot, targetType, targetId);
                    PersistSlot(sourceSlot);
                }
                else
                {
                    ApplyCommandToSlot(slot, type, id);
                    PersistSlot(slot);
                    ClearSlot(sourceSlot);
                }

                ResetDragState();
                return;
            }

            ApplyCommandToSlot(slot, type, id);
            PersistSlot(slot);
            ResetDragState();
        }
        catch (Exception exception)
        {
            Services.PluginLog.Exception(exception);
        }
    }

    private void OnSlotClicked(ExtraSlot slot)
    {
        try
        {
            ExecuteCommand(slot.CommandType, slot.CommandId);
        }
        catch (Exception exception)
        {
            Services.PluginLog.Exception(exception);
        }
    }

    private void OnSlotDiscarded(ExtraSlot slot)
    {
        try
        {
            if (suppressSourceDiscard && slot == dragSourceSlot)
            {
                ResetDragState();
                return;
            }

            ClearSlot(slot);
            ResetDragState();
        }
        catch (Exception exception)
        {
            Services.PluginLog.Exception(exception);
        }
    }

    private void ClearSlot(ExtraSlot slot)
    {
        ApplyCommandToSlot(slot, HotbarSlotType.Empty, 0);

        if (config is null) return;

        var slots = GetActiveSlots(createIfMissing: true);
        if (slots is null) return;

        slots.RemoveAll(entry =>
            entry.Page == currentPage && entry.Column == slot.Column && entry.Row == slot.Row);

        MirrorActiveSlotsToAllCharacters(slots);
        _ = config.Save();
    }

    private void PersistSlot(ExtraSlot slot)
    {
        if (config is null) return;

        var slots = GetActiveSlots(createIfMissing: true);
        if (slots is null) return;

        slots.RemoveAll(entry =>
            entry.Page == currentPage && entry.Column == slot.Column && entry.Row == slot.Row);
        slots.Add(new SavedSlotCommand
        {
            Page = currentPage,
            Column = slot.Column,
            Row = slot.Row,
            CommandType = (byte)slot.CommandType,
            CommandId = slot.CommandId,
        });

        MirrorActiveSlotsToAllCharacters(slots);
        _ = config.Save();
    }

    // Resolves a dropped payload into the hotbar command it represents, letting the game normalize
    // special drag types (inventory items, key items, crystals) into their concrete command.
    private static unsafe bool ResolveCommand(DragDropPayload payload, out HotbarSlotType type, out uint id)
    {
        type = HotbarSlotType.Empty;
        id = 0;

        var module = RaptureHotbarModule.Instance();
        if (module is null) return false;

        var scratch = &module->ScratchSlot;

        // Inventory-sourced drags (items, crystals, key items) carry addon-local grid coordinates in
        // Int1/Int2, NOT a hotbar command. They must be resolved against the live drag source and must
        // never fall through to the generic DragDropType mapping below, which would otherwise fabricate
        // a bogus macro out of the dragged item's grid slot index.
        if (IsInventoryDragType(payload.Type))
        {
            return TryResolveInventoryDrag(payload, scratch, out type, out id);
        }

        var setType = UIGlobals.GetHotbarSlotTypeFromDragDropType(payload.Type);
        if (setType is HotbarSlotType.Empty) return false;

        scratch->Set(setType, BuildSetCommandId(payload, setType));

        type = scratch->CommandType;
        id = scratch->CommandId;
        return IsValidCommand(type, id);
    }

    private static bool IsInventoryDragType(DragDropType type) => type is
        DragDropType.Item or DragDropType.Inventory_Item or DragDropType.RemoteInventory_Item or
        DragDropType.Crystal or DragDropType.Inventory_Crystal or DragDropType.EventItem;

    // The addon the active drag originated in (e.g. InventoryGrid2E, ArmouryBoard, InventoryBuddy).
    private static unsafe AtkUnitBase* GetActiveDragSourceAddon()
    {
        var stage = AtkStage.Instance();
        if (stage is null) return null;

        var source = stage->DragDropManager.DragDrop1;
        if (source is null) return null;

        var node = source->GetComponentNode();
        if (node is null) return null;

        return RaptureAtkUnitManager.Instance()->GetAddonByNode((AtkResNode*)node);
    }

    // Resolves an item dragged from an inventory grid into its concrete hotbar command. The drag
    // payload only carries the grid slot, so the real item is found via the source addon and the
    // ItemOrderModule sorter (which honours the displayed sort order), then handed to the game as an
    // InventoryItem so it normalizes HQ / collectable flags exactly like the native panel does.
    private static unsafe bool TryResolveDraggedInventoryItem(
        int gridSlot,
        RaptureHotbarModule.HotbarSlot* scratch,
        out HotbarSlotType type,
        out uint id)
    {
        type = HotbarSlotType.Empty;
        id = 0;

        var source = GetActiveDragSourceAddon();
        if (source is null) return false;

        // Inventory item grids are children of the inventory window; the sorter lives on the parent.
        var sorter = Inventory.GetSorterForInventory(source);
        if (sorter is null && source->ParentId is not 0)
        {
            var parent = RaptureAtkUnitManager.Instance()->GetAddonById(source->ParentId);
            sorter = Inventory.GetSorterForInventory(parent);
        }

        if (sorter is null) return false;

        var page = Inventory.GetAdjustedPage(source, gridSlot);
        var index = Inventory.GetAdjustedIndex(source, gridSlot);
        var displayIndex = index + sorter->ItemsPerPage * page;

        var item = sorter->GetInventoryItem(displayIndex);
        if (item is null || item->ItemId is 0) return false;

        var encodedId = ((uint)item->Container << 16) | ((uint)item->Slot & 0xFFFF);
        scratch->Set(HotbarSlotType.InventoryItem, encodedId);
        type = scratch->CommandType;
        id = scratch->CommandId;
        return IsValidCommand(type, id);
    }

    private static unsafe bool TryResolveInventoryDrag(
        DragDropPayload payload,
        RaptureHotbarModule.HotbarSlot* scratch,
        out HotbarSlotType type,
        out uint id)
    {
        type = HotbarSlotType.Empty;
        id = 0;

        if (payload.Int2 < 0) return false;

        switch (payload.Type)
        {
            case DragDropType.Item:
            case DragDropType.Inventory_Item:
            case DragDropType.RemoteInventory_Item:
            {
                // The payload only carries the grid slot (Int2); the real item must be looked up from
                // the inventory addon the drag originated in, honouring the ItemOrderModule sort order.
                return TryResolveDraggedInventoryItem(payload.Int2, scratch, out type, out id);
            }

            case DragDropType.Crystal:
            case DragDropType.Inventory_Crystal:
            {
                scratch->Set(HotbarSlotType.Crystal, (uint)payload.Int2);
                type = scratch->CommandType;
                id = scratch->CommandId;
                return IsValidCommand(type, id);
            }

            case DragDropType.EventItem:
            {
                scratch->Set(HotbarSlotType.KeyItem, (uint)payload.Int2);
                type = scratch->CommandType;
                id = scratch->CommandId;
                return IsValidCommand(type, id);
            }

            default:
                return false;
        }
    }

    private static uint BuildSetCommandId(DragDropPayload payload, HotbarSlotType setType)
        => setType switch
        {
            HotbarSlotType.InventoryItem => (uint)(payload.Int1 << 16 | (payload.Int2 & 0xFFFF)),
            HotbarSlotType.KeyItem or HotbarSlotType.Crystal => payload.Int2 < 0 ? 0u : (uint)payload.Int2,
            HotbarSlotType.Macro => ToMacroCommandId(payload),
            _ => payload.Int2 < 0 ? 0u : (uint)payload.Int2,
        };

    private static uint ToMacroCommandId(DragDropPayload payload)
    {
        if (payload.Int2 < 0) return 0;

        var macroId = (uint)payload.Int2;
        if (payload.Int1 == 1)
        {
            macroId += MacroSharedSetOffset;
        }

        return macroId;
    }

    private static unsafe uint GetCommandIcon(HotbarSlotType type, uint id)
    {
        if (!IsValidCommand(type, id)) return 0;

        var module = RaptureHotbarModule.Instance();
        if (module is null) return 0;

        var scratch = &module->ScratchSlot;
        scratch->Set(type, id);
        scratch->LoadIconId();
        return scratch->IconId;
    }

    private static unsafe void ExecuteCommand(HotbarSlotType type, uint id)
    {
        if (!IsValidCommand(type, id)) return;

        var module = RaptureHotbarModule.Instance();
        if (module is null) return;

        var scratch = &module->ScratchSlot;
        scratch->Set(type, id);
        module->ExecuteSlot(scratch);
    }

    // A command id of 0 is only valid for the first individual macro; for everything else 0 means empty.
    private static bool IsValidCommand(HotbarSlotType type, uint id)
        => type is not HotbarSlotType.Empty && (id is not 0 || type is HotbarSlotType.Macro);

    private static DragDropPayload BuildPayload(HotbarSlotType type, uint id)
    {
        var dragDropType = ToDragDropType(type);

        if (type is HotbarSlotType.Macro)
        {
            return new DragDropPayload
            {
                Type = dragDropType,
                Int1 = (int)(id / MacroSharedSetOffset),
                Int2 = (int)(id % MacroSharedSetOffset),
            };
        }

        return new DragDropPayload
        {
            Type = dragDropType,
            Int2 = (int)id,
        };
    }

    private static DragDropType ToDragDropType(HotbarSlotType type) => type switch
    {
        HotbarSlotType.Action => DragDropType.ActionBar_Action,
        HotbarSlotType.Item => DragDropType.ActionBar_Item,
        HotbarSlotType.EventItem => DragDropType.ActionBar_EventItem,
        HotbarSlotType.Emote => DragDropType.ActionBar_Emote,
        HotbarSlotType.Macro => DragDropType.ActionBar_Macro,
        HotbarSlotType.Marker => DragDropType.ActionBar_Marker,
        HotbarSlotType.FieldMarker => DragDropType.ActionBar_FieldMarker,
        HotbarSlotType.CraftAction => DragDropType.ActionBar_CraftingAction,
        HotbarSlotType.GeneralAction => DragDropType.ActionBar_GeneralAction,
        HotbarSlotType.BuddyAction => DragDropType.ActionBar_BuddyAction,
        HotbarSlotType.MainCommand => DragDropType.ActionBar_MainCommand,
        HotbarSlotType.ExtraCommand => DragDropType.ActionBar_ExtraCommand,
        HotbarSlotType.Companion => DragDropType.ActionBar_Companion,
        HotbarSlotType.Crystal => DragDropType.Crystal,
        HotbarSlotType.GearSet => DragDropType.ActionBar_GearSet,
        HotbarSlotType.PetAction => DragDropType.ActionBar_PetAction,
        HotbarSlotType.Mount => DragDropType.ActionBar_Mount,
        HotbarSlotType.Recipe => DragDropType.ActionBar_Recipe,
        HotbarSlotType.PvPQuickChat => DragDropType.ActionBar_QuickChat,
        HotbarSlotType.BgcArmyAction => DragDropType.ActionBar_BgcArmyAction,
        HotbarSlotType.PerformanceInstrument => DragDropType.ActionBar_Perform,
        HotbarSlotType.McGuffin => DragDropType.ActionBar_McGuffin,
        HotbarSlotType.Ornament => DragDropType.ActionBar_Ornament,
        HotbarSlotType.LostFindsItem => DragDropType.ActionBar_MYCTemporaryItem,
        HotbarSlotType.Glasses => DragDropType.ActionBar_Glasses,
        _ => DragDropType.Nothing,
    };

    private unsafe void ResizePanel(AtkUnitBase* addon, int columns, int rows)
    {
        var contentWidth = columns * SlotPitch - 2.0f;
        var contentHeight = rows * SlotPitch - 2.0f;

        var container = addon->GetNodeById(SlotContainerNodeId);
        if (container is not null)
        {
            container->Size = new Vector2(contentWidth, contentHeight);
        }

        var background = addon->GetNodeById(PanelBackgroundNodeId);
        if (background is not null)
        {
            background->Size = new Vector2(contentWidth + 32.0f, contentHeight + 28.0f);
        }

        ResizeWindow(addon, columns, rows);
    }

    private unsafe void ResizeWindow(AtkUnitBase* addon, int columns, int rows)
    {
        var windowWidth = BaseWindowWidth + (columns - NativeColumns) * SlotPitch;
        var windowHeight = BaseWindowHeight + (rows - NativeRows) * SlotPitch;
        var windowSize = new Vector2(windowWidth, windowHeight);

        var rootNode = addon->GetNodeById(RootNodeId);
        if (rootNode is not null)
        {
            rootNode->Size = windowSize;
        }

        var windowNode = addon->GetNodeById(WindowComponentNodeId);
        if (windowNode is not null)
        {
            windowNode->Size = windowSize;
        }

        var windowComponent = addon->GetComponentById<AtkComponentWindow>(WindowComponentNodeId);
        if (windowComponent is null) return;

        foreach (var nodeId in WindowFrameNodeIds)
        {
            var frameNode = windowComponent->GetNodeById(nodeId);
            if (frameNode is null) continue;

            frameNode->Size = windowSize;
        }

        var headerCollision = windowComponent->GetNodeById(HeaderCollisionNodeId);
        if (headerCollision is not null)
        {
            headerCollision->Size = new Vector2(windowWidth, HeaderHeight);
        }
    }
}
