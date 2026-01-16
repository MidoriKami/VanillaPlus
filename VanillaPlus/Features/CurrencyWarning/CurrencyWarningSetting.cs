using System;
using System.Text.RegularExpressions;

namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarningSetting {
    public uint ItemId;
    public WarningMode Mode = WarningMode.Above;
    public int Limit;

    public static bool IsSearchMatch(CurrencyWarningSetting item, string search) {
        var regex = new Regex(search, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        var itemData = Services.DataManager.GetItem(item.ItemId);

        return regex.IsMatch(itemData.Name.ToString());
    }

    public static int ItemComparer(CurrencyWarningSetting left, CurrencyWarningSetting right, string _) {
        var leftItem = Services.DataManager.GetItem(left.ItemId);
        var rightItem = Services.DataManager.GetItem(right.ItemId);

        return string.Compare(leftItem.Name.ToString(), rightItem.Name.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
