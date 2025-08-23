using System.Collections.Generic;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.StickyShopCategories;

public class StickyShopCategoriesData : GameModificationData<StickyShopCategoriesData> {
    protected override string FileName => "StickyShopCategories.data.json";

    public List<ShopConfig> ShopConfigs { get; set; } = [];

    public class ShopConfig {
        public uint ShopId { get; set; } = 0;
        public uint CategoryId { get; set; } = 0;
        public uint SubCategoryId { get; set; } = 0;
        public int CategoryIndex { get; set; } = 0;
        public int SubCategoryIndex { get; set; } = 0;
    }
}
