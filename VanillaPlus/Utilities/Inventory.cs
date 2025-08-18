using System.Collections.Generic;
using Dalamud.Game.Inventory;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace VanillaPlus.Utilities;

public static class Inventory {
    public static List<InventoryType> StandardInventories => [
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
        InventoryType.EquippedItems,
        InventoryType.ArmoryMainHand,
        InventoryType.ArmoryHead,
        InventoryType.ArmoryBody,
        InventoryType.ArmoryHands,
        InventoryType.ArmoryWaist,
        InventoryType.ArmoryLegs,
        InventoryType.ArmoryFeets,
        InventoryType.ArmoryOffHand,
        InventoryType.ArmoryEar,
        InventoryType.ArmoryNeck,
        InventoryType.ArmoryWrist,
        InventoryType.ArmoryRings,
        InventoryType.Currency,
        InventoryType.Crystals,
        InventoryType.ArmorySoulCrystal,
    ];

    public static bool Contains(this List<InventoryType> inventoryTypes, GameInventoryType type) 
        => inventoryTypes.Contains((InventoryType)type);
}
