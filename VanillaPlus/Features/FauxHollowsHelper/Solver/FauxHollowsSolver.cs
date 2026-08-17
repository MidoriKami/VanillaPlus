using System;
using System.Collections.Generic;
using System.Linq;

using LuminaSupplemental.Excel.Model;
using LuminaSupplemental.Excel.Services;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

// Adapted from https://github.com/JoshuaEN/ffxiv-faux-hollows.
public static class FauxHollowsSolver {
    private const int PresentWeightFactor = 4;
    private const int SwordWeightFactor = 6;
    private const int DisambiguationFactor = 1_000;
    private const double SmartFillWeightValue = 1_000_000;

    private static IReadOnlyList<FauxHollowsIdentifierPatterns>? identifiers;

    private static IReadOnlyList<FauxHollowsIdentifierPatterns> Identifiers
        => identifiers ?? throw new InvalidOperationException("Faux Hollows solver has not been initialized.");

    public static void Initialize()
        => identifiers ??= LoadIdentifiers();

    public static TileHint[] Solve(TileState[] board) {
        var solveState = CalculatedSolveState(board);

        var hints = new TileHint[BoundingBox.BoardCells];
        for (var index = 0; index < BoundingBox.BoardCells; index++) {
            hints[index] = ComputeHint(solveState, index);
        }

        return hints;
    }

    private static TileHint ComputeHint(SolveState solveState, int index) {
        var userState = solveState.GetUserState(index);
        if (userState is not TileState.Unknown) return TileHint.None;

        var smartFill = solveState.GetSmartFill(index);
        if (smartFill is not null) {
            return smartFill switch {
                TileState.Sword => TileHint.Known,
                TileState.Present => TileHint.Known,
                _ => TileHint.None,
            };
        }

        switch (solveState.SolveStep) {
            case SolveStep.FillSword: {
                var suggestion = solveState.GetSuggestion(index);
                return suggestion is { Sword: > 0 } ? TileHint.Recommended : TileHint.None;
            }

            case SolveStep.FillPresent: {
                var suggestion = solveState.GetSuggestion(index);
                return suggestion is { Present: > 0 } ? TileHint.Recommended : TileHint.None;
            }

            case SolveStep.SuggestTiles: {
                var finalWeight = solveState.GetFinalWeight(index);
                if (finalWeight is not null && Math.Abs(finalWeight.Value - solveState.MaxTileWeight) < 0.1f && solveState.MaxTileWeight > 0.0) {
                    return TileHint.Recommended;
                }

                return solveState.GetFoxOddsValue(index) > 0.249 ? TileHint.Fox : TileHint.None;
            }

            case SolveStep.SuggestFoxes: {
                return solveState.GetConfirmedFoxes(index) > 0 ? TileHint.Fox : TileHint.None;
            }

            default:
                return TileHint.None;
        }
    }

    private static SolveState CalculatedSolveState(TileState[] userSelected) {
        var solveState = new SolveState(userSelected);
        var blocked = solveState.UserStatesIndexList[TileState.Blocked];

        if (GetIdentifierCandidate(blocked) is not { } identifierCandidate) {
            return solveState.Finalize(SolveStep.FillBlocked);
        }

        if (CalculateStateCandidates(solveState, identifierCandidate.Patterns) is { } finalStep) {
            return solveState.Finalize(finalStep);
        }

        var mainShapesSolved = solveState.IsSolved(TileState.Present) && solveState.IsSolved(TileState.Sword);

        if (mainShapesSolved && !solveState.AnyFoxes()) {
            return solveState.Finalize(SolveStep.Done);
        }

        return solveState.Finalize(mainShapesSolved ? SolveStep.SuggestFoxes : SolveStep.SuggestTiles);
    }

    private static FauxHollowsIdentifierPatterns? GetIdentifierCandidate(IReadOnlyCollection<int> blocked)
        => Identifiers.FirstOrDefault(candidate =>
            candidate.Blocked.Count == blocked.Count &&
            candidate.Blocked.All(blocked.Contains));

    private static SolveStep? CalculateStateCandidates(SolveState solveState, IReadOnlyList<FauxHollowsPattern> patterns) {
        var shapes = new[] {
            (
                State: TileState.Sword,
                LongSide: 3,
                ShortSide: 2,
                Bounds: BoundingBox.FromIndexes(solveState.UserStatesIndexList[TileState.Sword])
            ),
            (
                State: TileState.Present,
                LongSide: 2,
                ShortSide: 2,
                Bounds: BoundingBox.FromIndexes(solveState.UserStatesIndexList[TileState.Present])
            ),
        };

        foreach (var shape in shapes) {
            if (shape.Bounds is null) continue;

            if (shape.Bounds.ShortSide > shape.ShortSide || shape.Bounds.LongSide > shape.LongSide) {
                return shape.State is TileState.Sword ? SolveStep.FillSword : SolveStep.FillPresent;
            }
        }

        var filteredPatterns = new List<ProcessedPattern>();
        foreach (var pattern in patterns) {
            var processed = new ProcessedPattern {
                Pattern = pattern,
                PresentBox = BoundingBox.ForPattern(pattern, TileState.Present),
                SwordBox = BoundingBox.ForPattern(pattern, TileState.Sword),
            };

            var skip = false;
            foreach (var shape in shapes) {
                foreach (var index in processed.BoxFor(shape.State).Indexes()) {
                    if (!solveState.CanPlaceStateAt(shape.State, index)) {
                        skip = true;
                        break;
                    }
                }
                if (skip) break;

                if (shape.Bounds is { } bounds && !processed.BoxFor(shape.State).Contains(bounds)) {
                    skip = true;
                    break;
                }
            }

            if (!skip) {
                filteredPatterns.Add(processed);
            }
        }

        if (filteredPatterns.Count == 0) {
            return SolveStep.FillSword;
        }

        var firstPattern = filteredPatterns[0].Pattern;

        var swordSolved = filteredPatterns.All(pattern =>
            pattern.Pattern.Sword == firstPattern.Sword &&
            pattern.Pattern.Sword3x2 == firstPattern.Sword3x2);

        var presentSolved = filteredPatterns.All(pattern =>
            pattern.Pattern.Present == firstPattern.Present);

        if (swordSolved) {
            solveState.SetSolved(TileState.Sword);
        }

        if (presentSolved) {
            solveState.SetSolved(TileState.Present);
        }

        foreach (var shape in shapes) {
            var commonIndexes = new Dictionary<int, int>();
            foreach (var pattern in filteredPatterns) {
                foreach (var index in pattern.BoxFor(shape.State).Indexes()) {
                    commonIndexes[index] = commonIndexes.GetValueOrDefault(index) + 1;
                }
            }

            foreach (var (index, count) in commonIndexes) {
                if (count == filteredPatterns.Count) {
                    solveState.SetSmartFill(index, shape.State);
                }
                else {
                    solveState.AddSuggestion(index, shape.State, count);
                }
            }
        }

        var candidatePatterns = filteredPatterns.Select(p => p.Pattern).ToList();
        solveState.SetCandidatePatternCount(candidatePatterns.Count);
        ApplyFoxSuggestions(candidatePatterns, solveState);
        SetFinalWeightsFromSuggestions(solveState);

        if (!swordSolved && solveState.UserStatesIndexList[TileState.Sword].Count > 0) {
            return SolveStep.FillSword;
        }

        if (!presentSolved && solveState.UserStatesIndexList[TileState.Present].Count > 0) {
            return SolveStep.FillPresent;
        }

        return null;
    }

    private static void ApplyFoxSuggestions(IReadOnlyList<FauxHollowsPattern> candidatePatterns, SolveState solveState) {
        if (solveState.UserStatesIndexList[TileState.Fox].Count != 0) return;

        foreach (var pattern in candidatePatterns) {
            foreach (var confirmedFox in pattern.ConfirmedFoxes) {
                if (solveState.IsEmptyAt(confirmedFox)) {
                    solveState.AddSuggestion(confirmedFox, TileState.Fox, 1);
                    solveState.AddConfirmedFoxOdd(confirmedFox, pattern.ConfirmedFoxes.Count);
                }
            }
        }
    }

    private static void SetFinalWeightsFromSuggestions(SolveState solveState) {
        var incompleteSword = IsIncompleteSmartFill(solveState, TileState.Sword);
        var incompletePresent = IsIncompleteSmartFill(solveState, TileState.Present);

        for (var index = 0; index < BoundingBox.BoardCells; index++) {
            var smartFill = solveState.GetSmartFill(index);

            if (smartFill is TileState.Sword && incompleteSword) {
                solveState.SetFinalWeight(index, SmartFillWeightValue);
            }
            else if (smartFill is TileState.Present && incompletePresent) {
                solveState.SetFinalWeight(index, SmartFillWeightValue);
            }
            else {
                var suggestion = solveState.GetSuggestion(index);
                if (suggestion is not null) {
                    solveState.SetFinalWeight(index, CalculateSuggestionWeight(suggestion.Value));
                }
            }
        }

        if (incompleteSword) {
            solveState.ResetSmartFillFor(TileState.Sword);
        }

        if (incompletePresent) {
            solveState.ResetSmartFillFor(TileState.Present);
        }
    }

    private static bool IsIncompleteSmartFill(SolveState solveState, TileState state)
        => !solveState.IsSolved(state) &&
           solveState.GetSmartFillReversedCount(state) > 0 &&
           solveState.UserStatesIndexList[state].Count is 0;

    private static double CalculateSuggestionWeight(TileSuggestion suggestion) {
        var finalPresentWeight = suggestion.Present * PresentWeightFactor;
        var finalSwordWeight = suggestion.Sword * SwordWeightFactor;

        return (finalPresentWeight + finalSwordWeight) * (double)DisambiguationFactor + suggestion.Fox;
    }

    private static IReadOnlyList<FauxHollowsIdentifierPatterns> LoadIdentifiers() {
        var patterns = CsvLoader.LoadResource<FauxHollowsPattern>(
            CsvLoader.FauxHollowsPatternResourceName,
            includesHeaders: true,
            out var failedLines,
            out var exceptions
        );

        if (failedLines.Count != 0 || exceptions.Count != 0 || patterns.Count == 0) {
            throw new InvalidOperationException("Unable to load Faux Hollows pattern data from LuminaSupplemental.");
        }

        return [
            .. patterns
                .GroupBy(pattern => pattern.Identifier)
                .Select(group => {
                    var first = group.First();
                    if (group.Any(pattern => !pattern.BlockedTiles.SequenceEqual(first.BlockedTiles))) {
                        throw new InvalidOperationException($"Faux Hollows identifier '{group.Key}' has inconsistent blocked tiles.");
                    }

                    return new FauxHollowsIdentifierPatterns(first.BlockedTiles, [.. group]);
                }),
        ];
    }
}
