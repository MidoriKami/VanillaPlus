using Lumina.Excel.Sheets;

namespace VanillaPlus.Extensions;

public static class TerritoryTypeExtensions {
    extension(TerritoryType territoryType) {
        public string LoadingImagePath {
            get {
                if (!Services.DataManager.GetExcelSheet<LoadingImage>().TryGetRow(territoryType.LoadingImage.RowId, out var loadingImage)) return string.Empty;

                var imageName = loadingImage.FileName.ExtractText();
                if (string.IsNullOrEmpty(imageName)) return string.Empty;

                return $"ui/loadingimage/{imageName}_hr1.tex";
            }
        }
    }
}
