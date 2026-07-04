using System.Collections.Generic;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

/// <summary>Axis-aligned bounding box over the 6x6 Faux Hollows board.</summary>
internal sealed class BoundingBox {
    public const int BoardWidth = 6;
    public const int BoardHeight = 6;
    public const int BoardCells = BoardWidth * BoardHeight;

    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public int ShortSide { get; }
    public int LongSide { get; }

    private int[]? indexes;

    public BoundingBox(int x, int y, int width, int height) {
        X = x;
        Y = y;
        Width = width;
        Height = height;

        if (width < height) {
            ShortSide = width;
            LongSide = height;
        }
        else {
            ShortSide = height;
            LongSide = width;
        }
    }

    public static int CordToIndex(int x, int y)
        => x + BoardHeight * y;

    public static (int X, int Y) IndexToCord(int index)
        => (index % BoardHeight, index / BoardHeight);

    public bool Contains(BoundingBox? other) {
        if (other is null) return false;

        return X <= other.X &&
               X + Width >= other.X + other.Width &&
               Y <= other.Y &&
               Y + Height >= other.Y + other.Height;
    }

    public IReadOnlyList<int> Indexes() {
        if (indexes is not null) return indexes;

        var result = new List<int>(Width * Height);
        for (var y = Y; y < Y + Height; y++) {
            for (var x = X; x < X + Width; x++) {
                result.Add(CordToIndex(x, y));
            }
        }

        indexes = result.ToArray();
        return indexes;
    }

    public static BoundingBox FromXYs(int minX, int minY, int maxX, int maxY)
        => new(minX, minY, maxX - minX + 1, maxY - minY + 1);

    /// <summary>Builds the smallest bounding box that contains all the given indexes, or null if empty.</summary>
    public static BoundingBox? FromIndexes(IReadOnlyCollection<int> boardIndexes) {
        if (boardIndexes.Count < 1) return null;

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = 0;
        var maxY = 0;

        foreach (var index in boardIndexes) {
            var (x, y) = IndexToCord(index);
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        return FromXYs(minX, minY, maxX, maxY);
    }

    /// <summary>Bounding box for a Sword or Present within the given pattern.</summary>
    public static BoundingBox ForPattern(CommunityDataPattern pattern, TileState state) {
        var index = state == TileState.Sword ? pattern.Sword : pattern.Present;
        var (x, y) = IndexToCord(index);

        var width = state == TileState.Sword && pattern.Sword3x2 ? 3 : 2;
        var height = state == TileState.Sword && !pattern.Sword3x2 ? 3 : 2;

        return new BoundingBox(x, y, width, height);
    }
}
