using System;
using System.Collections.Generic;
using LuminaSupplemental.Excel.Model;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

public sealed class BoundingBox(int x, int y, int width, int height) {
    public const int BoardWidth = 6;
    public const int BoardHeight = 6;
    public const int BoardCells = BoardWidth * BoardHeight;

    public int X { get; } = x;
    public int Y { get; } = y;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int ShortSide { get; } = Math.Min(width, height);
    public int LongSide { get; } = Math.Max(width, height);

    private int[]? indexes;

    private static int CoordinatesToIndex(int x, int y)
        => x + BoardWidth * y;

    private static (int X, int Y) IndexToCoordinates(int index)
        => (index % BoardWidth, index / BoardWidth);

    public bool Contains(BoundingBox other)
        => X <= other.X &&
           X + Width >= other.X + other.Width &&
           Y <= other.Y &&
           Y + Height >= other.Y + other.Height;

    public IReadOnlyList<int> Indexes() {
        if (indexes is not null) return indexes;

        indexes = new int[Width * Height];
        var index = 0;
        for (var y = Y; y < Y + Height; y++) {
            for (var x = X; x < X + Width; x++) {
                indexes[index++] = CoordinatesToIndex(x, y);
            }
        }

        return indexes;
    }

    public static BoundingBox? FromIndexes(IReadOnlyCollection<int> boardIndexes) {
        if (boardIndexes.Count < 1) return null;

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = 0;
        var maxY = 0;

        foreach (var index in boardIndexes) {
            var (x, y) = IndexToCoordinates(index);
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        return new BoundingBox(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public static BoundingBox ForPattern(FauxHollowsPattern pattern, TileState state) {
        var index = state == TileState.Sword ? pattern.Sword : pattern.Present;
        var (x, y) = IndexToCoordinates(index);

        var width = state == TileState.Sword && pattern.Sword3x2 ? 3 : 2;
        var height = state == TileState.Sword && !pattern.Sword3x2 ? 3 : 2;

        return new BoundingBox(x, y, width, height);
    }
}
