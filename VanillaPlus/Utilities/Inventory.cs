using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Inventory;
using FFXIVClientStructs.FFXIV.Client.Game;
using VanillaPlus.Classes;

namespace VanillaPlus.Utilities;

public static unsafe class Inventory {
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

    public static IEnumerable<ItemStack> GetInventoryStacks()
        => from itemGroup in GetInventoryItems().GroupBy(item => item.ItemId) 
           where itemGroup.Key is not 0
           let totalCount = itemGroup.Sum(item => item.Quantity) 
           let item = itemGroup.First() 
           select new ItemStack(item, totalCount);

    public static List<InventoryItem> GetInventoryItems() {
        List<InventoryType> inventories = [ InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4 ];
        List<InventoryItem> items = [];

        foreach (var inventory in inventories) {
            var container = InventoryManager.Instance()->GetInventoryContainer(inventory);

            for (var index = 0; index < container->Size; ++index) {
                ref var item = ref container->Items[index];
                if (item.ItemId is 0) continue;
                
                items.Add(item);
            }
        }

        return items;
    }
    
    public static List<InventoryItem> GetInventoryItems(string filterString, bool invert = false) 
        => GetInventoryItems().Where(item => item.IsRegexMatch(filterString) != invert).ToList();
}
