using System.Collections.Generic;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

internal sealed class CommunityDataPattern(int present, int sword, bool sword3x2, IReadOnlyList<int> confirmedFoxes) {
    public int Present { get; } = present;
    public int Sword { get; } = sword;
    public bool Sword3x2 { get; } = sword3x2;
    public IReadOnlyList<int> ConfirmedFoxes { get; } = confirmedFoxes;
}

internal sealed class CommunityDataIdentifierPatterns(string identifier, IReadOnlyList<int> blocked, IReadOnlyList<CommunityDataPattern> patterns) {
    public string Identifier { get; } = identifier;
    public IReadOnlyList<int> Blocked { get; } = blocked;
    public IReadOnlyList<CommunityDataPattern> Patterns { get; } = patterns;
}