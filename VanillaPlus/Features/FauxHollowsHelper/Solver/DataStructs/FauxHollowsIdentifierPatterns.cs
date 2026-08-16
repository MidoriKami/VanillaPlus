using System.Collections.Generic;
using LuminaSupplemental.Excel.Model;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

public class FauxHollowsIdentifierPatterns(IReadOnlyList<int> blocked, IReadOnlyList<FauxHollowsPattern> patterns) {
    public IReadOnlyList<int> Blocked { get; } = blocked;
    public IReadOnlyList<FauxHollowsPattern> Patterns { get; } = patterns;
}
