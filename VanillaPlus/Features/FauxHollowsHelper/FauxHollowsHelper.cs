using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.FauxHollowsHelper.Solver;

namespace VanillaPlus.Features.FauxHollowsHelper;

public class FauxHollowsHelper : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_FauxHollowsHelper,
        Description = Strings.ModificationDescription_FauxHollowsHelper,
        Authors = ["daemitus", "MidoriKami", "Glorou", "JoshuaEN", "MapleRecall"],
        Type = ModificationType.UserInterface,
        Tags = [
            IDataManager.Get().GetAddonText(13568).ExtractText(), // Faux Hollows
            "Fox",
        ],
        CompatibilityModule = new PluginCompatibilityModule("ezFauxHollows", "FauxHollowsSolver"),
    };

    private AddonController<AddonWeeklyPuzzle>? weeklyPuzzleController;

    private TileState[]? lastBoard;
    private List<RevealedTile>? lastReveals;
    private TileHint[]? lastHints;
    private bool updatePending;

    public override string ImageName => "FauxHollowsHelper.png";

    public override async Task OnEnableAsync() {
        FauxHollowsSolver.LoadDate();

        IAddonLifecycle.Get().RegisterListener(AddonEvent.PostReceiveEvent, "WeeklyPuzzle", OnWeeklyPuzzleReceiveEvent);

        unsafe {
            weeklyPuzzleController = new AddonController<AddonWeeklyPuzzle> {
                AddonName = "WeeklyPuzzle",
                OnSetup = SetupWeeklyPuzzle,
                OnFinalize = FinalizeWeeklyPuzzle,
                OnRefresh = RequestWeeklyPuzzleUpdate,
                OnUpdate = UpdateWeeklyPuzzleIfPending,
            };
        }

        await weeklyPuzzleController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        FauxHollowsSolver.UnloadData();

        IAddonLifecycle.Get().UnregisterListener(OnWeeklyPuzzleReceiveEvent);

        await weeklyPuzzleController.DisposeAsyncSafe();
        weeklyPuzzleController = null;

        lastBoard = null;
        lastReveals = null;
        lastHints = null;
        updatePending = false;
    }

    private unsafe void SetupWeeklyPuzzle(AddonWeeklyPuzzle* addon) {
        lastBoard = null;
        lastReveals = null;
        lastHints = null;
        updatePending = true;
    }

    private unsafe void FinalizeWeeklyPuzzle(AddonWeeklyPuzzle* addon) {
        ClearAllTints(addon);

        lastBoard = null;
        lastReveals = null;
        lastHints = null;
        updatePending = false;
    }

    private unsafe void UpdateWeeklyPuzzle(AddonWeeklyPuzzle* addon) {
        if (!addon->IsVisible || addon->UldManager.LoadedState != AtkLoadState.Loaded) return;

        var board = new TileState[BoundingBox.BoardCells];
        List<RevealedTile> reveals = [];

        for (var i = 0; i < board.Length; i++) {
            if (ReadTile(addon, i) is not { } state) return;

            board[i] = state;

            if (state is TileState.Present or TileState.Sword && ReadPrizeReveal(addon, i) is { } reveal) {
                reveals.Add(reveal);
            }
        }

        if (!BoardsEqual(lastBoard, board) || !RevealsEqual(lastReveals, reveals)) {
            lastHints = FauxHollowsHints.Compute(board, reveals);
            lastBoard = board;
            lastReveals = reveals;
        }

        if (lastHints is null) return;

        for (var i = 0; i < board.Length; i++) {
            ApplyTint(addon, i, lastHints[i]);
        }

        updatePending = false;
    }

    private unsafe void RequestWeeklyPuzzleUpdate(AddonWeeklyPuzzle* addon) {
        updatePending = true;
    }

    private unsafe void UpdateWeeklyPuzzleIfPending(AddonWeeklyPuzzle* addon) {
        if (!updatePending) return;

        UpdateWeeklyPuzzle(addon);
    }

    private void OnWeeklyPuzzleReceiveEvent(AddonEvent type, AddonArgs args) {
        if (args is not AddonReceiveEventArgs receiveEventArgs) return;
        if ((AtkEventType)receiveEventArgs.AtkEventType is not AtkEventType.TimelineActiveLabelChanged) return;

        updatePending = true;
    }

    private static bool BoardsEqual(TileState[]? left, TileState[] right) {
        if (left is null || left.Length != right.Length) return false;

        for (var i = 0; i < right.Length; i++) {
            if (left[i] != right[i]) return false;
        }

        return true;
    }

    private static bool RevealsEqual(List<RevealedTile>? left, List<RevealedTile> right) {
        if (left is null || left.Count != right.Count) return false;

        for (var i = 0; i < right.Count; i++) {
            if (!left[i].Equals(right[i])) return false;
        }

        return true;
    }

    private static unsafe TileState? ReadTile(AddonWeeklyPuzzle* addon, int index) {
        var button = addon->GameBoard[index / BoundingBox.BoardWidth][index % BoundingBox.BoardWidth].Button;
        if (button is null) return null;

        var backgroundNode = (AtkImageNode*)button->GetNodeById(10);
        if (backgroundNode is null) return null;

        switch ((WeeklyPuzzleTexture)backgroundNode->PartId) {
            case WeeklyPuzzleTexture.Hidden:
                return TileState.Unknown;

            case WeeklyPuzzleTexture.Blocked:
                return TileState.Blocked;

            case WeeklyPuzzleTexture.Blank: {
                    var iconNode = (AtkImageNode*)button->GetNodeById(8);
                    if (iconNode is null) return TileState.Empty;
                    if (!iconNode->IsVisible()) return TileState.Empty;

                    return (WeeklyPuzzlePrizeTexture)iconNode->PartId switch {
                        >= WeeklyPuzzlePrizeTexture.BoxUpperLeft and <= WeeklyPuzzlePrizeTexture.ChestLowerRight => TileState.Present,
                        >= WeeklyPuzzlePrizeTexture.SwordsUpperLeft and <= WeeklyPuzzlePrizeTexture.SwordsLowerRight => TileState.Sword,
                        WeeklyPuzzlePrizeTexture.TinyCommander or WeeklyPuzzlePrizeTexture.Commander => TileState.Fox,
                        _ => null,
                    };
                }

            default:
                return null;
        }
    }

    private static unsafe RevealedTile? ReadPrizeReveal(AddonWeeklyPuzzle* addon, int index) {
        var button = addon->GameBoard[index / BoundingBox.BoardWidth][index % BoundingBox.BoardWidth].Button;
        if (button is null) return null;

        var backgroundNode = (AtkImageNode*)button->GetNodeById(10);
        if (backgroundNode is null || (WeeklyPuzzleTexture)backgroundNode->PartId != WeeklyPuzzleTexture.Blank) {
            return null;
        }

        var iconNode = (AtkImageNode*)button->GetNodeById(8);
        if (iconNode is null || !iconNode->IsVisible()) return null;

        var part = (WeeklyPuzzlePrizeTexture)iconNode->PartId;
        if (part is < WeeklyPuzzlePrizeTexture.BoxUpperLeft or > WeeklyPuzzlePrizeTexture.SwordsLowerRight) return null;

        var rotation = iconNode->Rotation < 0.0f ? -1 : iconNode->Rotation > 0.0f ? 1 : 0;
        return new RevealedTile(index, part, rotation);
    }

    private static unsafe void ApplyTint(AddonWeeklyPuzzle* addon, int index, TileHint hint) {
        var button = addon->GameBoard[index / BoundingBox.BoardWidth][index % BoundingBox.BoardWidth].Button;
        if (button is null) return;

        var backgroundNode = (AtkImageNode*)button->GetNodeById(10);
        if (backgroundNode is null) return;

        backgroundNode->AtkResNode.AddColor = ResolveColor(hint).AsVector3Color();
    }

    private static Vector4 ResolveColor(TileHint hint) => hint switch {
        TileHint.Recommended => new Vector4(0.125f, 0.561f, 0.180f, 1.0f),
        TileHint.Known => new Vector4(0.125f, 0.314f, 0.627f, 1.0f),
        TileHint.Fox => new Vector4(0.706f, 0.471f, 0.0f, 1.0f),
        _ => Vector4.Zero,
    };

    private static unsafe void ClearAllTints(AddonWeeklyPuzzle* addon) {
        for (var i = 0; i < BoundingBox.BoardCells; i++) {
            ApplyTint(addon, i, TileHint.None);
        }
    }
}
