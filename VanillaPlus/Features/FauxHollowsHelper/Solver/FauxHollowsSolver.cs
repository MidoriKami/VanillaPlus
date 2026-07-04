using System.Collections.Generic;
using System.Linq;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

/// <summary>
/// Community-data Faux Hollows solver (ported from https://github.com/JoshuaEN/ffxiv-faux-hollows).
/// </summary>
public sealed class FauxHollowsSolver {
    // Weight factors (see s6p4-f1-weighter.ts)
    private const int PresentWeightFactor = 4;
    private const int SwordWeightFactor = 6;
    private const int DisambiguationFactor = 1_000;
    private const double SmartFillWeightValue = 1_000_000;

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
        if (userState != TileState.Unknown) {
            // Already shown in-game.
            return TileHint.None;
        }

        var smartFill = solveState.GetSmartFill(index);
        if (smartFill is not null) {
            return smartFill switch {
                TileState.Sword => TileHint.Known,
                TileState.Present => TileHint.Known,
                _ => TileHint.None, // Blocked is already visible in-game.
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
                if (finalWeight is not null && finalWeight.Value == solveState.MaxTileWeight && solveState.MaxTileWeight > 0.0) {
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

        var identifierCandidates = GetIdentifierCandidates(blocked);

        // Blocked tiles do not match any known pattern (e.g. board is mid-transition).
        if (identifierCandidates.Count != 1) {
            if (identifierCandidates.Count > 1) {
                solveState.SetCandidatePatterns(identifierCandidates.SelectMany(p => p.Patterns).ToList());
            }
            return solveState.Finalize(SolveStep.FillBlocked);
        }

        var identifierCandidate = identifierCandidates[0];
        foreach (var index in identifierCandidate.Blocked) {
            solveState.SetSmartFill(index, TileState.Blocked);
        }

        var result = CalculateStatesCandidates(solveState, identifierCandidate.Patterns);
        if (result.FinalizedSolveState is not null) {
            return result.FinalizedSolveState;
        }

        var mainShapesSolved = result.SolvedPresent > -1 && result.SolvedSword > -1;
        solveState.SetCandidatePatterns(result.CandidatePatterns);

        if (mainShapesSolved && !solveState.AnyFoxes()) {
            return solveState.Finalize(SolveStep.Done);
        }

        return solveState.Finalize(mainShapesSolved ? SolveStep.SuggestFoxes : SolveStep.SuggestTiles);
    }

    private static List<CommunityDataIdentifierPatterns> GetIdentifierCandidates(IReadOnlyCollection<int> blocked) {
        var result = new List<CommunityDataIdentifierPatterns>();
        if (blocked.Count == 0) return result;

        foreach (var candidate in CommunityData.Identifiers) {
            if (candidate.Blocked.Count != blocked.Count) continue;
            if (candidate.Blocked.All(blocked.Contains)) {
                result.Add(candidate);
            }
        }

        return result;
    }

    private sealed class ShapeData {
        public required TileState State { get; init; }
        public required int LongSide { get; init; }
        public required int ShortSide { get; init; }
        public required BoundingBox? BoundingBox { get; init; }
    }

    private sealed class ProcessedPattern {
        public required CommunityDataPattern Pattern { get; init; }
        public required BoundingBox PresentBox { get; init; }
        public required BoundingBox SwordBox { get; init; }

        public BoundingBox BoxFor(TileState state)
            => state == TileState.Sword ? SwordBox : PresentBox;
    }

    private readonly struct StateCandidatesResult {
        public int SolvedPresent { get; init; }
        public int SolvedSword { get; init; }
        public SolveState? FinalizedSolveState { get; init; }
        public List<CommunityDataPattern> CandidatePatterns { get; init; }
    }

    private static StateCandidatesResult CalculateStatesCandidates(SolveState solveState, IReadOnlyList<CommunityDataPattern> patterns) {
        var shapes = new[] {
            new ShapeData {
                State = TileState.Sword,
                LongSide = 3,
                ShortSide = 2,
                BoundingBox = BoundingBox.FromIndexes(solveState.UserStatesIndexList[TileState.Sword]),
            },
            new ShapeData {
                State = TileState.Present,
                LongSide = 2,
                ShortSide = 2,
                BoundingBox = BoundingBox.FromIndexes(solveState.UserStatesIndexList[TileState.Present]),
            },
        };

        var solvedPresent = -1;
        var solvedSword = -1;

        foreach (var shape in shapes) {
            solveState.ResetSuggestionsFor(shape.State);
        }

        // Validate the user input is logical for each shape.
        foreach (var shape in shapes) {
            if (shape.BoundingBox is null) continue;

            if (shape.BoundingBox.ShortSide > shape.ShortSide || shape.BoundingBox.LongSide > shape.LongSide) {
                return new StateCandidatesResult {
                    SolvedPresent = solvedPresent,
                    SolvedSword = solvedSword,
                    FinalizedSolveState = solveState.Finalize(shape.State == TileState.Sword ? SolveStep.FillSword : SolveStep.FillPresent),
                    CandidatePatterns = [],
                };
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
                    if (solveState.GetStateEligibility(shape.State, index) == StateTileEligibility.Taken) {
                        skip = true;
                        break;
                    }
                }
                if (skip) break;

                if (shape.BoundingBox is not null && !processed.BoxFor(shape.State).Contains(shape.BoundingBox)) {
                    skip = true;
                    break;
                }
            }

            if (!skip) {
                filteredPatterns.Add(processed);
            }
        }

        ProcessedPattern? onlySword = filteredPatterns.Count > 0 ? filteredPatterns[0] : null;
        ProcessedPattern? onlyPresent = filteredPatterns.Count > 0 ? filteredPatterns[0] : null;

        while (filteredPatterns.Count > 1) {
            var loopStartSword = onlySword;
            var loopStartPresent = onlyPresent;

            onlySword ??= filteredPatterns[0];
            onlyPresent ??= filteredPatterns[0];

            for (var i = 1; i < filteredPatterns.Count; i++) {
                var pattern = filteredPatterns[i].Pattern;
                if (onlySword is null ||
                    onlySword.Pattern.Sword != pattern.Sword ||
                    onlySword.Pattern.Sword3x2 != pattern.Sword3x2) {
                    onlySword = null;
                }

                if (onlyPresent is null || onlyPresent.Pattern.Present != pattern.Present) {
                    onlyPresent = null;
                }
            }

            if (ReferenceEquals(loopStartSword, onlySword) && ReferenceEquals(loopStartPresent, onlyPresent)) {
                break;
            }

            var sword = onlySword;
            var present = onlyPresent;
            filteredPatterns = filteredPatterns.Where(p =>
                (sword is null || (sword.Pattern.Sword == p.Pattern.Sword && sword.Pattern.Sword3x2 == p.Pattern.Sword3x2)) &&
                (present is null || present.Pattern.Present == p.Pattern.Present)).ToList();
        }

        if (filteredPatterns.Count == 0) {
            return new StateCandidatesResult {
                SolvedPresent = solvedPresent,
                SolvedSword = solvedSword,
                FinalizedSolveState = solveState.Finalize(SolveStep.FillSword),
                CandidatePatterns = [],
            };
        }

        if (onlySword is not null) {
            solvedSword = onlySword.Pattern.Sword;
            solveState.SetSolved(TileState.Sword, true);
        }

        if (onlyPresent is not null) {
            solvedPresent = onlyPresent.Pattern.Present;
            solveState.SetSolved(TileState.Present, true);
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
        ApplyFoxSuggestions(candidatePatterns, solveState);
        SetFinalWeightsFromSuggestions(solveState);

        SolveState? finalized = null;
        if (onlySword is null && solveState.UserStatesIndexList[TileState.Sword].Count > 0) {
            finalized = solveState.Finalize(SolveStep.FillSword);
        }
        else if (onlyPresent is null && solveState.UserStatesIndexList[TileState.Present].Count > 0) {
            finalized = solveState.Finalize(SolveStep.FillPresent);
        }

        return new StateCandidatesResult {
            SolvedPresent = solvedPresent,
            SolvedSword = solvedSword,
            FinalizedSolveState = finalized,
            CandidatePatterns = candidatePatterns,
        };
    }

    private static void ApplyFoxSuggestions(IReadOnlyList<CommunityDataPattern> candidatePatterns, SolveState solveState) {
        // If the user has already entered a fox, there is nothing to suggest.
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
            if (smartFill == TileState.Sword && incompleteSword) {
                solveState.AddSuggestion(index, TileState.Sword, 1);
                solveState.SetFinalWeight(index, SmartFillWeightValue);
            }
            else if (smartFill == TileState.Present && incompletePresent) {
                solveState.AddSuggestion(index, TileState.Present, 1);
                solveState.SetFinalWeight(index, SmartFillWeightValue);
            }
            else {
                var suggestion = solveState.GetSuggestion(index);
                if (suggestion is not null) {
                    solveState.SetFinalWeight(index, CalculateSuggestionWeight(suggestion.Value));
                }
            }
        }

        if (incompleteSword) solveState.ResetSmartFillFor(TileState.Sword);
        if (incompletePresent) solveState.ResetSmartFillFor(TileState.Present);
    }

    private static bool IsIncompleteSmartFill(SolveState solveState, TileState state)
        => !solveState.GetSolved(state) &&
           solveState.GetSmartFillReversedCount(state) > 0 &&
           solveState.UserStatesIndexList[state].Count == 0;

    private static double CalculateSuggestionWeight(TileSuggestion suggestion) {
        var finalPresentWeight = suggestion.Present * PresentWeightFactor;
        var finalSwordWeight = suggestion.Sword * SwordWeightFactor;
        return (finalPresentWeight + finalSwordWeight) * (double)DisambiguationFactor + suggestion.Fox;
    }
}
