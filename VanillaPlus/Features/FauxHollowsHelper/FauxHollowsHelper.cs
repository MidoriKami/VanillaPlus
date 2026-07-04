using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using VanillaPlus.Features.FauxHollowsHelper.Solver;
using Exception = System.Exception;

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

    private static readonly Vector4 RecommendedColor = new(0.125f, 0.561f, 0.180f, 1.0f);
    private static readonly Vector4 KnownColor = new(0.125f, 0.314f, 0.627f, 1.0f);
    private static readonly Vector4 FoxColor = new(0.706f, 0.471f, 0.0f, 1.0f);

    private AddonController<AddonWeeklyPuzzle>? weeklyPuzzleController;

    private TileState[]? lastBoard;
    private List<RevealedTile>? lastReveals;

    public override string ImageName => "FauxHollowsHelper.png";

    public override async Task OnEnableAsync() {
        unsafe {
            weeklyPuzzleController = new AddonController<AddonWeeklyPuzzle> {
                AddonName = "WeeklyPuzzle",
                OnSetup = SetupWeeklyPuzzle,
                OnFinalize = FinalizeWeeklyPuzzle,
                OnUpdate = UpdateWeeklyPuzzle,
            };
        }

        await IFramework.Get().Run(weeklyPuzzleController.Enable);
    }

    public override async Task OnDisableAsync() {
        await IFramework.Get().Run(() => weeklyPuzzleController?.Dispose());
        weeklyPuzzleController = null;

        lastBoard = null;
        lastReveals = null;
    }

    private unsafe void SetupWeeklyPuzzle(AddonWeeklyPuzzle* addon) {
        lastBoard = null;
        lastReveals = null;
    }

    private unsafe void FinalizeWeeklyPuzzle(AddonWeeklyPuzzle* addon) {
        ClearAllTints(addon);

        lastBoard = null;
        lastReveals = null;
    }

    private unsafe void UpdateWeeklyPuzzle(AddonWeeklyPuzzle* addon) {
        if (addon is null) return;
        if (!addon->IsVisible || addon->UldManager.LoadedState != AtkLoadState.Loaded) return;

        try {
            var board = new TileState[BoundingBox.BoardCells];
            var reveals = new List<RevealedTile>();
            for (var i = 0; i < board.Length; i++) {
                if (ReadTile(addon, i) is not { } state) return;
                board[i] = state;
                if (state is TileState.Present or TileState.Sword &&
                    ReadPrizeReveal(addon, i) is { } reveal) {
                    reveals.Add(reveal);
                }
            }

            if (BoardsEqual(lastBoard, board) && RevealsEqual(lastReveals, reveals)) return;

            var hints = FauxHollowsHints.Compute(board, reveals);
            for (var i = 0; i < board.Length; i++) {
                ApplyTint(addon, i, hints[i]);
            }

            lastBoard = board;
            lastReveals = reveals;
        }
        catch (Exception ex) {
            IPluginLog.Get().Exception(ex);
        }
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

        var backgroundNode = (AtkImageNode*)button->UldManager.NodeList[3];
        if (backgroundNode is null) return null;

        switch ((WeeklyPuzzleTexture)backgroundNode->PartId) {
            case WeeklyPuzzleTexture.Hidden:
                return TileState.Unknown;

            case WeeklyPuzzleTexture.Blocked:
                return TileState.Blocked;

            case WeeklyPuzzleTexture.Blank: {
                    var iconNode = (AtkImageNode*)button->UldManager.NodeList[6];
                    if (iconNode is null || !iconNode->IsVisible()) return TileState.Empty;

                    return (WeeklyPuzzlePrizeTexture)iconNode->PartId switch {
                        >= WeeklyPuzzlePrizeTexture.BoxUpperLeft and <= WeeklyPuzzlePrizeTexture.ChestLowerRight => TileState.Present,
                        >= WeeklyPuzzlePrizeTexture.SwordsUpperLeft and <= WeeklyPuzzlePrizeTexture.SwordsLowerRight => TileState.Sword,
                        WeeklyPuzzlePrizeTexture.TinyCommander
                            or WeeklyPuzzlePrizeTexture.Commander => TileState.Fox,
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

        var backgroundNode = (AtkImageNode*)button->UldManager.NodeList[3];
        if (backgroundNode is null || (WeeklyPuzzleTexture)backgroundNode->PartId != WeeklyPuzzleTexture.Blank) {
            return null;
        }

        var iconNode = (AtkImageNode*)button->UldManager.NodeList[6];
        if (iconNode is null || !iconNode->IsVisible()) return null;

        var part = (WeeklyPuzzlePrizeTexture)iconNode->PartId;
        if (part is < WeeklyPuzzlePrizeTexture.BoxUpperLeft or > WeeklyPuzzlePrizeTexture.SwordsLowerRight) return null;

        var rotation = iconNode->Rotation < 0.0f ? -1 : iconNode->Rotation > 0.0f ? 1 : 0;
        return new RevealedTile(index, part, rotation);
    }

    private static unsafe void ApplyTint(AddonWeeklyPuzzle* addon, int index, TileHint hint) {
        var button = addon->GameBoard[index / BoundingBox.BoardWidth][index % BoundingBox.BoardWidth].Button;
        if (button is null) return;

        var backgroundNode = (AtkImageNode*)button->UldManager.NodeList[3];
        if (backgroundNode is null) return;

        var color = ResolveColor(hint);
        backgroundNode->AddRed = (short)(color.X * 255.0f);
        backgroundNode->AddGreen = (short)(color.Y * 255.0f);
        backgroundNode->AddBlue = (short)(color.Z * 255.0f);
    }

    private static Vector4 ResolveColor(TileHint hint) => hint switch {
        TileHint.Recommended => RecommendedColor,
        TileHint.Known => KnownColor,
        TileHint.Fox => FoxColor,
        _ => Vector4.Zero,
    };

    private static unsafe void ClearAllTints(AddonWeeklyPuzzle* addon) {
        if (addon is null) return;

        for (var i = 0; i < BoundingBox.BoardCells; i++) {
            ApplyTint(addon, i, TileHint.None);
        }
    }
}