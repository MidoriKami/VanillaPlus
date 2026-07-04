namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

public enum TileState {
    Unknown,
    Empty,
    Blocked,

    Present,
    Sword,
    Fox,
}

public enum TileHint {
    None,
    Recommended,
    Fox,
    Known,
}