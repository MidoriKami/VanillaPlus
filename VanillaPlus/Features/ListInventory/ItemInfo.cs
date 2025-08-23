using System;
using System.Numerics;
using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using VanillaPlus.Extensions;

namespace VanillaPlus.Features.ListInventory;

public class ItemInfo : IEquatable<ItemInfo> {
    public required InventoryItem Item { get; set; }
    public required int ItemCount { get; set; }

    private Item ItemData => Services.DataManager.GetExcelSheet<Item>().GetRow(Item.ItemId);

    public Vector4 RarityColor => ItemData.RarityColor();

    public uint IconId => ItemData.Icon;
    
    public string Name => ItemData.Name.ToString();
    
    public int Level => ItemData.LevelEquip;

    public int ItemLevel => (int) ItemData.LevelItem.RowId;
    
    public int Rarity => ItemData.Rarity;

    public int UiCategory => (int) ItemData.ItemUICategory.RowId;
    
    private string Description => ItemData.Description.ToString();
    
    public bool IsRegexMatch(string searchTerms) {
        const RegexOptions regexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase;
        
        if (Regex.IsMatch(Name, searchTerms, regexOptions)) return true;
        if (Regex.IsMatch(Description, searchTerms, regexOptions)) return true;
        if (Regex.IsMatch(Level.ToString(), searchTerms, regexOptions)) return true;
        if (Regex.IsMatch(ItemLevel.ToString(), searchTerms, regexOptions)) return true;

        return false;
    }

    public bool Equals(ItemInfo? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Item.ItemId.Equals(other.Item.ItemId) && ItemCount == other.ItemCount;
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ItemInfo) obj);
    }

    public override int GetHashCode()
        // ReSharper disable NonReadonlyMemberInGetHashCode
        => HashCode.Combine(Item.ItemId, ItemCount);
        // ReSharper restore NonReadonlyMemberInGetHashCode
}
