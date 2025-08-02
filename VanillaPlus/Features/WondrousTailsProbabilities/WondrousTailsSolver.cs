using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace VanillaPlus.WondrousTailsProbabilities;

/// <summary>
/// Minigame solver. Developed by Daemitris.
/// </summary>
public sealed partial class PerfectTails {
    private static readonly Random Random = new();
    private readonly Dictionary<int, long[]> possibleBoards = [];
    private readonly Dictionary<int, double[]> sampleProbabilities = [];

    public readonly bool[] GameState = new bool[16];
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PerfectTails"/> class.
    /// </summary>
    public PerfectTails() {
        CalculateBoards(0, 0, 0, 0, 0);
        CalculateSamples();
    }

    private static double[] Error { get; } = [-1, -1, -1];

    private double[] Solve(bool[] cells) {
        var counts = Values(cells);

        if (counts == null)
            return Error;

        var divisor = (double)counts[0];
        var probabilities = counts.Skip(1).Select(c => Math.Round(c / divisor, 4)).ToArray();

        return probabilities;
    }

    private double[] GetSample(int stickersPlaced)
        => sampleProbabilities.GetValueOrDefault(stickersPlaced, Error);

    private long[]? Values(bool[] cells)
        => possibleBoards.GetValueOrDefault(CellsToMask(cells));

    private long[] CalculateBoards(int mask, int numStickers, int numRows, int numCols, int numDiags) {
        if (possibleBoards.TryGetValue(mask, out var result))
            return result;

        if (numStickers is 9) {
            var lines = numRows + numCols + numDiags;
            return possibleBoards[mask] = [
                1,
                lines >= 1 ? 1 : 0,
                lines >= 2 ? 1 : 0,
                lines >= 3 ? 1 : 0,
            ];
        }

        if (numStickers > 9) {
            return possibleBoards[mask] = [0, 0, 0, 0];
        }

        result = possibleBoards[mask] = [0, 0, 0, 0];

        for (var r = 0; r < 4; r++) {
            for (var c = 0; c < 4; c++) {
                if (MaskHasBit(mask, r, c))
                    continue;

                var nMask = SetMaskBit(mask, r, c);
                var nRows = MaskHasRow(nMask, r) ? 1 : 0;
                var nCols = MaskHasCol(nMask, c) ? 1 : 0;
                var nDiag1 = MaskHasDiag1(nMask) && r == c ? 1 : 0;
                var nDiag2 = MaskHasDiag2(nMask) && r == 3 - c ? 1 : 0;
                var nResult = CalculateBoards(nMask, numStickers + 1, numRows + nRows, numCols + nCols, numDiags + nDiag1 + nDiag2);

                for (var i = 0; i < 4; i++)
                {
                    result[i] += nResult[i];
                }
            }
        }

        return result;
    }

    private void CalculateSamples() {
        for (var stickersPlaced = 1; stickersPlaced <= 7; stickersPlaced++) {
            var samples = new List<double[]>();
            for (var i = 0; i < 500; i++) {
                var sampleState = new bool[16];
                var sampleIndexes = Enumerable.Range(0, 16)
                    .OrderBy(_ => Random.Next())
                    .Take(stickersPlaced);

                foreach (var sampleIndex in sampleIndexes)
                    sampleState[sampleIndex] = true;

                samples.Add(Solve(sampleState));
            }

            sampleProbabilities[stickersPlaced] = [
                Math.Round(samples.Average(s => s[0]), 4),
                Math.Round(samples.Average(s => s[1]), 4),
                Math.Round(samples.Average(s => s[2]), 4),
            ];
        }
    }
}

/// <summary>
/// Static calculations.
/// </summary>
public sealed partial class PerfectTails {
    private static int CellsToMask(bool[] cells) {
        var mask = 0;
        for (var r = 0; r < 4; r++) {
            for (var c = 0; c < 4; c++) {
                if (cells[(r * 4) + c])
                    mask = SetMaskBit(mask, r, c);
            }
        }

        return mask;
    }

    private static int GetMaskBit(int r, int c)
        => 1 << ((4 * r) + c);

    private static int SetMaskBit(int mask, int r, int c)
        => mask | GetMaskBit(r, c);

    private static bool MaskHasBit(int mask, int r, int c)
        => (mask & GetMaskBit(r, c)) == GetMaskBit(r, c);

    private static bool MaskHasRow(int mask, int r)
        => Enumerable.Range(0, 4).All(c => MaskHasBit(mask, r, c));

    private static bool MaskHasCol(int mask, int c)
        => Enumerable.Range(0, 4).All(r => MaskHasBit(mask, r, c));

    private static bool MaskHasDiag1(int mask)
        => Enumerable.Range(0, 4).All(i => MaskHasBit(mask, i, i));

    private static bool MaskHasDiag2(int mask)
        => Enumerable.Range(0, 4).All(i => MaskHasBit(mask, i, 3 - i));
}

/// <summary>
/// Getting formatted results
/// </summary>
public sealed unsafe partial class PerfectTails {
    public SeString SolveAndGetProbabilitySeString() {
        var stickersPlaced = PlayerState.Instance()->WeeklyBingoNumPlacedStickers;

        // > 9 returns Error {-1,-1,-1} by the solver
        var values = Solve(GameState);

        double[]? samples = null;
        if (stickersPlaced is > 0 and <= 7)
            samples = GetSample(stickersPlaced);

        if (values == Error) {
            return new SeStringBuilder()
                .AddText("Line Chances: ")
                .AddUiForeground("error ", 704)
                .AddUiForeground("error ", 704)
                .AddUiForeground("error ", 704)
                .Build();
        }

        var valuePayloads = StringFormatDoubles(values);
        var seString = new SeStringBuilder()
            .AddText("Line Chances: ");

        if (samples != null) {
            foreach (var (value, sample, valuePayload) in Enumerable.Range(0, values.Length).Select(i => (values[i], samples[i], valuePayloads[i]))) {
                const double bound = 0.05;
                var sampleBoundLower = Math.Max(0, sample - bound);
                // var sampleBoundUpper = Math.Min(1, sample + bound);

                if (Math.Abs(value - 1) < 0.1f)
                    seString.AddUiGlow(valuePayload, 2);
                else if (value < 1 && value >= sample)
                    seString.AddUiForeground(valuePayload, 67);
                else if (sample > value && value > sampleBoundLower)
                    seString.AddUiForeground(valuePayload, 66);
                else if (sampleBoundLower > value && value > 0)
                    seString.AddUiForeground(valuePayload, 561);
                else if (value == 0)
                    seString.AddUiForeground(valuePayload, 704);
                else
                    seString.AddText(valuePayload);

                seString.AddText("  ");
            }

            seString.AddText("\rShuffle Average: ");
            seString.AddText(string.Join(" ", StringFormatDoubles(samples)));
        }
        else {
            seString.AddText(string.Join(" ", valuePayloads));
        }
        
        return seString.Build();
    }

    private string[] StringFormatDoubles(IEnumerable<double> values)
        => values.Select(v => $"{v * 100:F2}%").ToArray();
}
