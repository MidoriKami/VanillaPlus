using System.Numerics;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.ShowAetherCurrents;

public readonly record struct AetherCurrentInfo(RowRef<AetherCurrent> RowData, Level LevelData) {
    public Vector2 Position => new(LevelData.X, LevelData.Z);
    public uint MapId => LevelData.Map.RowId;
}
