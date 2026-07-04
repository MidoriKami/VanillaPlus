using System.Collections.Generic;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

internal enum SolveStep {
    FillBlocked,
    FillSword,
    FillPresent,
    SuggestTiles,
    SuggestFoxes,
    Done,
}

internal struct TileSuggestion {
    public int Present;
    public int Sword;
    public int Fox;
}

internal struct FoxOdds {
    public int ConfirmedFoxes;
    public int TotalFoxesForPatterns;
}

internal sealed class SolveState {
    private readonly TileState[] userStates;

    private readonly Dictionary<int, TileState> smartFills = [];
    private readonly Dictionary<TileState, List<int>> smartFillsReverse = new() {
        [TileState.Sword] = [],
        [TileState.Present] = [],
    };

    private readonly Dictionary<int, TileSuggestion> suggestions = [];
    private readonly Dictionary<int, double> finalWeights = [];
    private readonly Dictionary<int, FoxOdds> foxOdds = [];
    private readonly HashSet<TileState> solved = [];
    private int candidatePatternCount;

    public IReadOnlyDictionary<TileState, HashSet<int>> UserStatesIndexList { get; }

    public SolveStep SolveStep { get; private set; }
    public double MaxTileWeight { get; private set; }

    public SolveState(TileState[] userSelectedStates) {
        userStates = userSelectedStates;

        var indexList = new Dictionary<TileState, HashSet<int>> {
            [TileState.Blocked] = [],
            [TileState.Present] = [],
            [TileState.Sword] = [],
            [TileState.Fox] = [],
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

    public double? GetFinalWeight(int index)
        => finalWeights.TryGetValue(index, out var weight) ? weight : null;

    public bool IsSolved(TileState state)
        => solved.Contains(state);

    public bool AnyFoxes()
        => foxOdds.Count > 0;

    public void SetCandidatePatternCount(int count)
        => candidatePatternCount = count;

    public void SetSolved(TileState state)
        => solved.Add(state);

    public void SetSmartFill(int index, TileState state) {
        var currentState = GetSmartFill(index);

        var userState = userStates[index];
        if (userState != TileState.Unknown) return;

        if (currentState == state) return;

        smartFills[index] = state;

        if (currentState is not null && smartFillsReverse.TryGetValue(currentState.Value, out var previousList)) {
            previousList.Remove(index);
        }
        smartFillsReverse[state].Add(index);
    }

    public void AddSuggestion(int index, TileState state, int value) {
        var suggestion = suggestions.TryGetValue(index, out var existing) ? existing : new TileSuggestion();

        switch (state) {
            case TileState.Present: suggestion.Present += value; break;
            case TileState.Sword: suggestion.Sword += value; break;
            case TileState.Fox: suggestion.Fox += value; break;
        }

        suggestions[index] = suggestion;
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
    }

    public bool IsEmptyAt(int index)
        => userStates[index] == TileState.Unknown && GetSmartFill(index) is null;

    public bool CanPlaceStateAt(TileState state, int index) {
        var userSetState = userStates[index];
        var smartFillState = GetSmartFill(index);

        return userSetState == state ||
               smartFillState == state ||
               (userSetState == TileState.Unknown && smartFillState is null);
    }

    public double GetFoxOddsValue(int index) {
        if (!foxOdds.TryGetValue(index, out var details)) return 0.0;

        var foxesOnIndex = details.ConfirmedFoxes;
        if (candidatePatternCount == 0 || details.TotalFoxesForPatterns == 0) return 0.0;

        var oddsOfPatternHavingFox = (double)foxesOnIndex / candidatePatternCount;
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
