using System.Collections.Generic;
using System.Linq;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

internal enum SolveStep {
    FillBlocked,
    FillSword,
    FillPresent,
    SuggestTiles,
    SuggestFoxes,
    Done,
}

internal enum StateTileEligibility {
    Taken,
    Empty,

    /// <summary>Taken by the state being queried (not the Present shape).</summary>
    Present,
}

internal struct TileSuggestion {
    public int Blocked;
    public int Present;
    public int Sword;
    public int Fox;
}

internal struct FoxOdds {
    public int ConfirmedFoxes;
    public int TotalFoxesForPatterns;
}

/// <summary>Mutable solver working state.</summary>
internal sealed class SolveState {
    private readonly TileState[] userStates;

    private readonly Dictionary<int, TileState> smartFills = [];
    private readonly Dictionary<TileState, List<int>> smartFillsReverse = new() {
        [TileState.Sword] = [],
        [TileState.Present] = [],
        [TileState.Blocked] = [],
    };

    private readonly Dictionary<int, TileSuggestion> suggestions = [];
    private readonly Dictionary<int, double> finalWeights = [];
    private readonly Dictionary<int, FoxOdds> foxOdds = [];
    private int foxCount;

    private readonly Dictionary<TileState, bool> solved = new() {
        [TileState.Sword] = false,
        [TileState.Present] = false,
    };

    public IReadOnlyDictionary<TileState, HashSet<int>> UserStatesIndexList { get; }

    public SolveStep SolveStep { get; private set; }
    public double MaxTileWeight { get; private set; }
    public IReadOnlyList<CommunityDataPattern>? CandidatePatterns { get; private set; }

    public SolveState(TileState[] userSelectedStates) {
        userStates = (TileState[])userSelectedStates.Clone();

        var indexList = new Dictionary<TileState, HashSet<int>> {
            [TileState.Blocked] = [],
            [TileState.Present] = [],
            [TileState.Sword] = [],
            [TileState.Fox] = [],
            [TileState.Empty] = [],
        };

        for (var index = 0; index < userStates.Length; index++) {
            if (indexList.TryGetValue(userStates[index], out var set)) {
                set.Add(index);
            }
        }

        UserStatesIndexList = indexList;
    }

    public TileState GetUserState(int index)
        => userStates[index];

    public TileState? GetSmartFill(int index)
        => smartFills.TryGetValue(index, out var state) ? state : null;

    public int GetSmartFillReversedCount(TileState state)
        => smartFillsReverse[state].Count;

    public TileSuggestion? GetSuggestion(int index)
        => suggestions.TryGetValue(index, out var suggestion) ? suggestion : null;

    public FoxOdds? GetFoxOdds(int index)
        => foxOdds.TryGetValue(index, out var odds) ? odds : null;

    public double? GetFinalWeight(int index)
        => finalWeights.TryGetValue(index, out var weight) ? weight : null;

    public bool GetSolved(TileState state)
        => solved[state];

    public bool AnyFoxes()
        => foxCount > 0;

    public int? TotalCandidatePatterns
        => CandidatePatterns?.Count;

    public void SetCandidatePatterns(IReadOnlyList<CommunityDataPattern> patterns)
        => CandidatePatterns = patterns;

    public void SetSolved(TileState state, bool value)
        => solved[state] = value;

    public bool SetSmartFill(int index, TileState state) {
        var currentState = GetSmartFill(index);

        var userState = userStates[index];
        if (userState != TileState.Unknown) {
            // Avoid setting smart fill when the user has already provided this tile.
            return false;
        }

        if (currentState == state) return false;

        smartFills[index] = state;

        if (currentState is not null && smartFillsReverse.TryGetValue(currentState.Value, out var previousList)) {
            previousList.Remove(index);
        }

        smartFillsReverse[state].Add(index);
        return true;
    }

    public void AddSuggestion(int index, TileState state, int value) {
        var suggestion = suggestions.TryGetValue(index, out var existing) ? existing : new TileSuggestion();

        switch (state) {
            case TileState.Blocked: suggestion.Blocked += value; break;
            case TileState.Present: suggestion.Present += value; break;
            case TileState.Sword: suggestion.Sword += value; break;
            case TileState.Fox: suggestion.Fox += value; break;
        }

        suggestions[index] = suggestion;
    }

    public void ResetSuggestionsFor(TileState state) {
        foreach (var index in suggestions.Keys.ToList()) {
            var suggestion = suggestions[index];
            switch (state) {
                case TileState.Blocked: suggestion.Blocked = 0; break;
                case TileState.Present: suggestion.Present = 0; break;
                case TileState.Sword: suggestion.Sword = 0; break;
                case TileState.Fox: suggestion.Fox = 0; break;
            }
            suggestions[index] = suggestion;
        }
    }

    public void ResetSmartFillFor(TileState state) {
        foreach (var index in smartFillsReverse[state]) {
            smartFills.Remove(index);
        }
        smartFillsReverse[state].Clear();
    }

    public void SetFinalWeight(int index, double value)
        => finalWeights[index] = value;

    public void AddConfirmedFoxOdd(int index, int totalFoxesForPattern) {
        var previous = foxOdds.TryGetValue(index, out var existing) ? existing : new FoxOdds();
        foxOdds[index] = new FoxOdds {
            ConfirmedFoxes = previous.ConfirmedFoxes + 1,
            TotalFoxesForPatterns = previous.TotalFoxesForPatterns + totalFoxesForPattern,
        };
        foxCount += 1;
    }

    public bool IsEmptyAt(int index)
        => userStates[index] == TileState.Unknown && GetSmartFill(index) is null;

    public StateTileEligibility GetStateEligibility(TileState state, int index) {
        var userSetState = userStates[index];
        var smartFillState = GetSmartFill(index);

        if (userSetState == state || smartFillState == state) return StateTileEligibility.Present;
        if (userSetState == TileState.Unknown && smartFillState is null) return StateTileEligibility.Empty;
        return StateTileEligibility.Taken;
    }

    /// <summary>The odds a fox is on the given index (0 if none).</summary>
    public double GetFoxOddsValue(int index) {
        if (!foxOdds.TryGetValue(index, out var details)) return 0.0;

        var foxesOnIndex = details.ConfirmedFoxes;
        var total = TotalCandidatePatterns ?? 0;
        if (total == 0 || details.TotalFoxesForPatterns == 0) return 0.0;

        var oddsOfPatternHavingFox = (double)foxesOnIndex / total;
        var oddsOfTileHavingFox = (double)foxesOnIndex / details.TotalFoxesForPatterns;
        return oddsOfPatternHavingFox * oddsOfTileHavingFox;
    }

    public int GetConfirmedFoxes(int index)
        => foxOdds.TryGetValue(index, out var details) ? details.ConfirmedFoxes : 0;

    public SolveState Finalize(SolveStep solveStep) {
        var maxWeight = 0.0;
        for (var index = 0; index < BoundingBox.BoardCells; index++) {
            var weight = GetFinalWeight(index);
            if (weight is not null && weight.Value > maxWeight) {
                maxWeight = weight.Value;
            }
        }

        MaxTileWeight = maxWeight;
        SolveStep = solveStep;
        return this;
    }
}
