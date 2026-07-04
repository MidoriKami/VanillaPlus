using System.Collections.Generic;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

/// <summary>One possible board arrangement (Present/Sword placement and fox spots) for a blocked-tile pattern.</summary>
internal sealed class CommunityDataPattern(int present, int sword, bool sword3x2, IReadOnlyList<int> confirmedFoxes) {
    public int Present { get; } = present;
    public int Sword { get; } = sword;

    /// <summary>True when the Sword is 3 wide x 2 tall, false when 2 wide x 3 tall.</summary>
    public bool Sword3x2 { get; } = sword3x2;

    public IReadOnlyList<int> ConfirmedFoxes { get; } = confirmedFoxes;
}

/// <summary>The set of patterns associated with one arrangement of blocked tiles.</summary>
internal sealed class CommunityDataIdentifierPatterns(string identifier, IReadOnlyList<int> blocked, IReadOnlyList<CommunityDataPattern> patterns) {
    public string Identifier { get; } = identifier;
    public IReadOnlyList<int> Blocked { get; } = blocked;
    public IReadOnlyList<CommunityDataPattern> Patterns { get; } = patterns;
}