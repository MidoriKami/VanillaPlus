using System;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.CurrencyOverlay;

public class CurrencySetting : IComparable<CurrencySetting> {
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

    public int CompareTo(CurrencySetting? other) {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : ItemId.CompareTo(other.ItemId);
    }

    public bool IsMatch(string searchString) {
        var regex = new Regex(searchString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var itemData = Services.DataManager.GetExcelSheet<Item>().GetRow(ItemId);
        
        return regex.IsMatch(itemData.Name.ToString());
    }
}
