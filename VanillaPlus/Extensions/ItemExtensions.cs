﻿using System.Numerics;
using KamiToolKit.Classes;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Extensions;

public static class ItemExtensions {
    public static Vector4 RarityColor(this Item item) => item.Rarity switch {
        7 => ColorHelper.GetColor(561),
        4 => ColorHelper.GetColor(555),
        3 => ColorHelper.GetColor(553),
        2 => ColorHelper.GetColor(551),
        1 => ColorHelper.GetColor(549),
        _ => Vector4.One,
    };
}
