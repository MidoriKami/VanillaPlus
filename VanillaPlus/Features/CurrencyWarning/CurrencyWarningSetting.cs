using System;
using System.Text.RegularExpressions;
using Dalamud.Plugin.Services;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarningSetting {
    public uint ItemId;
    public WarningMode Mode = WarningMode.Above;
    public int Limit;

    public static bool IsSearchMatch(CurrencyWarningSetting item, string search) {
        var regex = new Regex(search, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        var itemData = Services.GetService<IDataManager>().GetItem(item.ItemId);

        return regex.IsMatch(itemData.Name.ToString());
    }

    public static int ItemComparer(CurrencyWarningSetting left, CurrencyWarningSetting right, Enum sortingMode) {
        var leftItem = Services.GetService<IDataManager>().GetItem(left.ItemId);
        var rightItem = Services.GetService<IDataManager>().GetItem(right.ItemId);

        return string.Compare(leftItem.Name.ToString(), rightItem.Name.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
