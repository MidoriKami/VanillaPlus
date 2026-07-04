using System.Collections.Generic;
using VanillaPlus.Features.FauxHollowsHelper.Solver;

namespace VanillaPlus.Features.FauxHollowsHelper;

internal readonly record struct RevealedTile(int Index, WeeklyPuzzlePrizeTexture Part, int Rotation);

// Shape reconstruction adapted from https://github.com/Glorou/EzFauxHollows.
internal static class FauxHollowsHints {
    private const int Width = BoundingBox.BoardWidth;
    private const int Height = BoundingBox.BoardHeight;

    internal static TileHint[] Compute(TileState[] board, IEnumerable<RevealedTile> reveals) {
        var solverBoard = (TileState[])board.Clone();

        foreach (var reveal in reveals) {
            if (Reconstruct(reveal.Part, reveal.Rotation, reveal.Index) is not { } shape) continue;

            var valid = true;
            foreach (var tile in shape.Indices) {
                if (solverBoard[tile] != TileState.Unknown && solverBoard[tile] != shape.State) {
                    valid = false;
                    break;
                }
            }
            if (!valid) continue;

            foreach (var tile in shape.Indices) {
                if (solverBoard[tile] == TileState.Unknown) {
                    solverBoard[tile] = shape.State;
                }
            }
        }

        var hints = FauxHollowsSolver.Solve(solverBoard);
        for (var i = 0; i < hints.Length; i++) {
            if (board[i] == TileState.Unknown && solverBoard[i] != TileState.Unknown) {
                hints[i] = TileHint.Known;
            }
        }

        return hints;
    }

    private readonly record struct ShapeMatch(TileState State, int[] Indices);

    private static ShapeMatch? Reconstruct(WeeklyPuzzlePrizeTexture part, int rotation, int index) {
        if (part is >= WeeklyPuzzlePrizeTexture.SwordsUpperLeft and <= WeeklyPuzzlePrizeTexture.SwordsLowerRight) {
            var indices = rotation == 0
                ? GetRect(SwordUpperLeft(part, rotation, index), 2, 3)
                : GetRect(SwordUpperLeft(part, rotation, index), 3, 2);
            return indices is null ? null : new ShapeMatch(TileState.Sword, indices);
        }

        if (part is >= WeeklyPuzzlePrizeTexture.BoxUpperLeft and <= WeeklyPuzzlePrizeTexture.ChestLowerRight) {
            var indices = GetRect(BoxUpperLeft(part, rotation, index), 2, 2);
            return indices is null ? null : new ShapeMatch(TileState.Present, indices);
        }

        return null;
    }

    private static int SwordUpperLeft(WeeklyPuzzlePrizeTexture part, int rotation, int i) => (part, rotation) switch {
        (WeeklyPuzzlePrizeTexture.SwordsUpperLeft, 0) => i,
        (WeeklyPuzzlePrizeTexture.SwordsUpperRight, 0) => i - 1,
        (WeeklyPuzzlePrizeTexture.SwordsMiddleLeft, 0) => i - Width,
        (WeeklyPuzzlePrizeTexture.SwordsMiddleRight, 0) => i - 1 - Width,
        (WeeklyPuzzlePrizeTexture.SwordsLowerLeft, 0) => i - (Width * 2),
        (WeeklyPuzzlePrizeTexture.SwordsLowerRight, 0) => i - 1 - (Width * 2),

        (WeeklyPuzzlePrizeTexture.SwordsUpperLeft, -1) => i - Width,
        (WeeklyPuzzlePrizeTexture.SwordsUpperRight, -1) => i,
        (WeeklyPuzzlePrizeTexture.SwordsMiddleLeft, -1) => i - 1 - Width,
        (WeeklyPuzzlePrizeTexture.SwordsMiddleRight, -1) => i - 1,
        (WeeklyPuzzlePrizeTexture.SwordsLowerLeft, -1) => i - 2 - Width,
        (WeeklyPuzzlePrizeTexture.SwordsLowerRight, -1) => i - 2,

        (WeeklyPuzzlePrizeTexture.SwordsUpperLeft, 1) => i - 2,
        (WeeklyPuzzlePrizeTexture.SwordsUpperRight, 1) => i - 2 - Width,
        (WeeklyPuzzlePrizeTexture.SwordsMiddleLeft, 1) => i - 1,
        (WeeklyPuzzlePrizeTexture.SwordsMiddleRight, 1) => i - 1 - Width,
        (WeeklyPuzzlePrizeTexture.SwordsLowerLeft, 1) => i,
        (WeeklyPuzzlePrizeTexture.SwordsLowerRight, 1) => i - Width,

        _ => -1,
    };

    private static int BoxUpperLeft(WeeklyPuzzlePrizeTexture part, int rotation, int i) {
        var corner = part switch {
            WeeklyPuzzlePrizeTexture.BoxUpperLeft or WeeklyPuzzlePrizeTexture.ChestUpperLeft => Corner.UpperLeft,
            WeeklyPuzzlePrizeTexture.BoxUpperRight or WeeklyPuzzlePrizeTexture.ChestUpperRight => Corner.UpperRight,
            WeeklyPuzzlePrizeTexture.BoxLowerLeft or WeeklyPuzzlePrizeTexture.ChestLowerLeft => Corner.LowerLeft,
            WeeklyPuzzlePrizeTexture.BoxLowerRight or WeeklyPuzzlePrizeTexture.ChestLowerRight => Corner.LowerRight,
            _ => Corner.Invalid,
        };

        return (corner, rotation) switch {
            (Corner.UpperLeft, 0) => i,
            (Corner.UpperRight, 0) => i - 1,
            (Corner.LowerLeft, 0) => i - Width,
            (Corner.LowerRight, 0) => i - 1 - Width,

            (Corner.UpperLeft, -1) => i - Width,
            (Corner.UpperRight, -1) => i,
            (Corner.LowerLeft, -1) => i - 1 - Width,
            (Corner.LowerRight, -1) => i - 1,

            (Corner.UpperLeft, 1) => i - 1,
            (Corner.UpperRight, 1) => i - 1 - Width,
            (Corner.LowerLeft, 1) => i,
            (Corner.LowerRight, 1) => i - Width,

            _ => -1,
        };
    }

    private static int[]? GetRect(int upperLeftIndex, int width, int height) {
        if (upperLeftIndex < 0) return null;

        var column = upperLeftIndex % Width;
        var row = upperLeftIndex / Width;
        if (column + width > Width || row + height > Height) return null;

        var indices = new int[width * height];
        var cursor = 0;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                indices[cursor++] = upperLeftIndex + x + (y * Width);
            }
        }

        return indices;
    }

    private enum Corner {
        Invalid,
        UpperLeft,
        UpperRight,
        LowerLeft,
        LowerRight,
    }
}