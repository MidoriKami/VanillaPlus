using Lumina.Excel;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.FancyLoadingScreens;

/// <summary>
/// Minimal definition of the game's "LoadingImage" Excel sheet, which Lumina does not generate.
/// Exposes the single texture-name column referenced by <c>TerritoryType.LoadingImage</c>.
/// </summary>
[Sheet("LoadingImage")]
public readonly struct LoadingImage(ExcelPage page, uint offset, uint row) : IExcelRow<LoadingImage> {
    public uint RowId => row;
    public uint RowOffset => offset;
    public ExcelPage ExcelPage => page;

    public ReadOnlySeString Name => page.ReadString(offset, offset);

    static LoadingImage IExcelRow<LoadingImage>.Create(ExcelPage page, uint offset, uint row)
        => new(page, offset, row);
}
