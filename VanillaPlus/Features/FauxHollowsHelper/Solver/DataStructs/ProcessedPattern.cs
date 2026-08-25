using LuminaSupplemental.Excel.Model;

namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

public sealed class ProcessedPattern {
    public required FauxHollowsPattern Pattern { get; init; }
    public required BoundingBox PresentBox { get; init; }
    public required BoundingBox SwordBox { get; init; }

    public BoundingBox BoxFor(TileState state)
        => state == TileState.Sword ? SwordBox : PresentBox;
}
