using System.Linq;
using System.Numerics;
using Dalamud.Utility;
using KamiToolKit.Premade.Addons;
using Lumina.Excel.Sheets;

namespace VanillaPlus.NativeElements.Addons.SearchAddons;

public static class TerritorySearchAddon {
    public static LuminaSearchAddon<TerritoryType> GetAddon() => new () {
        Size = new Vector2(350.0f, 600.0f),
        InternalName = "TerritorySearch",
        Title = "Zone Search",
        SearchOptions = Services.DataManager.GetExcelSheet<TerritoryType>()
            .Where(territory => territory.LoadingImage.RowId is not 0)
            .Where(territory => !territory.PlaceName.Value.Name.ToString().IsNullOrEmpty())
            .ToList(),

        GetLabelFunc = territory => territory.PlaceName.Value.Name.ToString(),
        GetIconIdFunc = _ => 60072,
        GetTexturePathFunc = territory => $"ui/loadingimage/{territory.LoadingImage.Value.FileName}_hr1.tex",
        GetSubLabelFunc = territory => territory.ContentFinderCondition.RowId is 0 ? string.Empty : territory.ContentFinderCondition.Value.Name.ToString(),

        SortingOptions = [ "Alphabetical" , "Id" ],
    };
}
