using System.Numerics;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        Authors = ["daemitus", "MidoriKami", "Glorou", "MapleRecall"],
        Type = ModificationType.UserInterface,
        Tags = [
            Services.DataManager.GetAddonText(13568).ExtractText(), // Faux Hollows
            Services.DataManager.GetAddonText(13561).ExtractText(), // Commander
            "Fox",
        ],
        CompatibilityModule = new PluginCompatibilityModule("ezFauxHollows", "FauxHollowsSolver"),
    };

    private static readonly Vector4 RecommendedColor = new(0.125f, 0.561f, 0.180f, 1.0f);
    private static readonly Vector4 KnownColor = new(0.125f, 0.314f, 0.627f, 1.0f);
    private static readonly Vector4 FoxColor = new(0.706f, 0.471f, 0.0f, 1.0f);

    private AddonController<AddonWeeklyPuzzle>? weeklyPuzzleController;
    private FauxHollowsSolver? solver;

    private TileState[]? lastBoard;
    private List<RevealedTile>? lastReveals;

    public override string ImageName => "FauxHollowsHelper.png";

    public override async Task OnEnableAsync() {
        solver = new FauxHollowsSolver();

        unsafe {
            weeklyPuzzleController = new AddonController<AddonWeeklyPuzzle> {
                AddonName = "WeeklyPuzzle",
                OnSetup = SetupWeeklyPuzzle,
                OnFinalize = FinalizeWeeklyPuzzle,
                OnUpdate = UpdateWeeklyPuzzle,
            };
        }

        await Services.Framework.RunSafely(weeklyPuzzleController.Enable);
    }

    public override async Task OnDisableAsync() {
        await Services.Framework.RunSafely(() => weeklyPuzzleController?.Dispose());
        weeklyPuzzleController = null;

        solver = null;
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
        if (addon is null || solver is null) return;
        if (!addon->IsVisible || addon->UldManager.LoadedState != AtkLoadState.Loaded) return;

        try {
            var board = new TileState[BoundingBox.BoardCells];
            for (var i = 0; i < board.Length; i++) {
                board[i] = ReadTile(addon, i);
            }

            // The exact revealed prize part (+ rotation) pins a whole shape from a single reveal.
            var reveals = new List<RevealedTile>();
            for (var i = 0; i < board.Length; i++) {
                if (ReadPrizeReveal(addon, i) is { } reveal) {
                    reveals.Add(new RevealedTile(i, reveal.Part, reveal.Rotation));
                }
            }

            if (BoardsEqual(lastBoard, board) && RevealsEqual(lastReveals, reveals)) return;

            var hints = FauxHollowsHints.Compute(solver, board, reveals);
            for (var i = 0; i < board.Length; i++) {
                ApplyTint(addon, i, hints[i]);
            }

            lastBoard = board;
            lastReveals = reveals;
        }
        catch (Exception ex) {
            Services.PluginLog.Exception(ex);
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

    private static unsafe TileState ReadTile(AddonWeeklyPuzzle* addon, int index) {
        var button = addon->GameBoard[index / 6][index % 6].Button;
        if (button is null) return TileState.Unknown;

        var backgroundNode = (AtkImageNode*)button->UldManager.NodeList[3];
        if (backgroundNode is null) return TileState.Unknown;

        switch ((WeeklyPuzzleTexture)backgroundNode->PartId) {
            case WeeklyPuzzleTexture.Hidden:
                return TileState.Unknown;

            case WeeklyPuzzleTexture.Blocked:
                return TileState.Blocked;

            case WeeklyPuzzleTexture.Blank: {
                    var iconNode = (AtkImageNode*)button->UldManager.NodeList[6];
                    if (iconNode is null || !iconNode->IsVisible()) return TileState.Empty;

                    return (WeeklyPuzzlePrizeTexture)iconNode->PartId switch {
                        WeeklyPuzzlePrizeTexture.TinyBox
                            or WeeklyPuzzlePrizeTexture.TinyChest
                            or (>= WeeklyPuzzlePrizeTexture.BoxUpperLeft and <= WeeklyPuzzlePrizeTexture.ChestLowerRight) => TileState.Present,
                        WeeklyPuzzlePrizeTexture.TinySwords
                            or (>= WeeklyPuzzlePrizeTexture.SwordsUpperLeft and <= WeeklyPuzzlePrizeTexture.SwordsLowerRight) => TileState.Sword,
                        WeeklyPuzzlePrizeTexture.TinyCommander
                            or WeeklyPuzzlePrizeTexture.Commander => TileState.Fox,
                        _ => TileState.Empty,
                    };
                }

            default:
                return TileState.Unknown;
        }
    }

    private readonly record struct PrizeReveal(WeeklyPuzzlePrizeTexture Part, int Rotation);

    /// <summary>Reads a tile's revealed prize sub-part and rotation (-1/0/+1), or null when it is not a revealed prize.</summary>
    private static unsafe PrizeReveal? ReadPrizeReveal(AddonWeeklyPuzzle* addon, int index) {
        var button = addon->GameBoard[index / 6][index % 6].Button;
        if (button is null) return null;

        var backgroundNode = (AtkImageNode*)button->UldManager.NodeList[3];
        if (backgroundNode is null || (WeeklyPuzzleTexture)backgroundNode->PartId != WeeklyPuzzleTexture.Blank) {
            return null;
        }

        var iconNode = (AtkImageNode*)button->UldManager.NodeList[6];
        if (iconNode is null || !iconNode->IsVisible()) return null;

        var rotation = iconNode->Rotation < 0.0f ? -1 : iconNode->Rotation > 0.0f ? 1 : 0;
        return new PrizeReveal((WeeklyPuzzlePrizeTexture)iconNode->PartId, rotation);
    }

    private static unsafe void ApplyTint(AddonWeeklyPuzzle* addon, int index, TileHint hint) {
        var button = addon->GameBoard[index / 6][index % 6].Button;
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
            var button = addon->GameBoard[i / 6][i % 6].Button;
            if (button is null) continue;

            var backgroundNode = (AtkImageNode*)button->UldManager.NodeList[3];
            if (backgroundNode is null) continue;

            backgroundNode->AddRed = 0;
            backgroundNode->AddGreen = 0;
            backgroundNode->AddBlue = 0;
        }
    }
}