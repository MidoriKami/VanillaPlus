using System.Collections.Generic;

using LuminaSupplemental.Excel.Model;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

internal sealed class FauxHollowsIdentifierPatterns(string identifier, IReadOnlyList<int> blocked, IReadOnlyList<FauxHollowsPattern> patterns) {
    public string Identifier { get; } = identifier;
    public IReadOnlyList<int> Blocked { get; } = blocked;
    public IReadOnlyList<FauxHollowsPattern> Patterns { get; } = patterns;
}
