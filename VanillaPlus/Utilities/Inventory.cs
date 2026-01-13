using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Inventory;
using FFXIVClientStructs.FFXIV.Client.Game;

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

        // todo use IGroupedEnumerable and de-clutter this
        // List<InventoryItem> itemInfos = [];
        // // itemInfos.AddRange(from itemGroups in items.GroupBy(item => item.ItemId)
        // //                    where itemGroups.Key is not 0
        // //                    let item = itemGroups.First()
        // //                    let itemCount = itemGroups.Sum(duplicateItem => duplicateItem.Quantity)
        // //                    select new ItemInfo {
        // //                        Item = item, ItemCount = itemCount,
        // //                    });
        //
        // return itemInfos;
    }
    
    public static List<InventoryItem> GetInventoryItems(string filterString, bool invert = false) 
        => GetInventoryItems().Where(item => item.IsRegexMatch(filterString) != invert).ToList();
}
