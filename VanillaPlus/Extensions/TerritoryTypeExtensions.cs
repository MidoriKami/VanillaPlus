using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Extensions;

public static class TerritoryTypeExtensions {
    extension(TerritoryType territoryType) {
        public string LoadingImagePath {
            get {
                if (!Services.DataManager.GetExcelSheet<LoadingImage>().TryGetRow(territoryType.LoadingImage.RowId, out var loadingImage)) return string.Empty;

                var imageName = loadingImage.Name.ExtractText();
                if (string.IsNullOrEmpty(imageName)) return string.Empty;

                return $"ui/loadingimage/{imageName}_hr1.tex";
            }
        }
    }
}

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

