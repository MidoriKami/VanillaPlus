using System;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.CurrencyOverlay;

public class CurrencySetting {
    public uint ItemId;
    public Vector2 Position = Vector2.Zero;
    public bool EnableLowLimit;
    public bool EnableHighLimit;
    public int LowLimit;
    public int HighLimit;
    public bool IconReversed;
    public bool TextReversed;
    public float Scale = 1.0f;
    public float FadePercent;
    public bool FadeIfNoWarnings;

    [JsonIgnore] public bool IsNodeMoveable;

    public static int Comparison(CurrencySetting left, CurrencySetting right, string mode) {
        switch (mode) {
            case "Alphabetical":
                var leftItem = Services.DataManager.GetItem(left.ItemId);
                var rightItem = Services.DataManager.GetItem(right.ItemId);
                return string.Compare(leftItem.Name.ToString(), rightItem.Name.ToString(), StringComparison.OrdinalIgnoreCase);
            
            case "Id":
                return left.ItemId.CompareTo(right.ItemId);
        }

        return 0;
    }

    public static bool IsMatch(CurrencySetting item, string searchString) {
        var regex = new Regex(searchString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var itemData = Services.DataManager.GetExcelSheet<Item>().GetRow(item.ItemId);
        
        return regex.IsMatch(itemData.Name.ToString());
    }
}
