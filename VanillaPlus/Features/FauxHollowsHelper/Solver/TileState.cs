namespace VanillaPlus.Features.FauxHollowsHelper.Solver;

/// <summary>Coarse state of a board tile, as understood by the solver.</summary>
public enum TileState {
    Unknown,
    Empty,
    Blocked,

    /// <summary>Part of the 2x2 chest / coffer reward.</summary>
    Present,

    /// <summary>Part of the 2x3 swords reward.</summary>
    Sword,
    Fox,
}

/// <summary>The kind of highlight to draw over a tile.</summary>
public enum TileHint {
    None,
    Recommended,
    Fox,
    Known,
}