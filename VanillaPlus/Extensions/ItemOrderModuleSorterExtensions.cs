using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace VanillaPlus.Extensions;

public static unsafe class ItemOrderModuleSorterExtensions {
    public static long GetSlotIndex(this ref ItemOrderModuleSorter sorter, ItemOrderModuleSorterItemEntry* entry)
        => entry->Slot + sorter.ItemsPerPage * entry->Page;
    
    public static InventoryItem* GetInventoryItem(ref this ItemOrderModuleSorter sorter, ItemOrderModuleSorterItemEntry* entry)
        => sorter.GetInventoryItem(sorter.GetSlotIndex(entry));
    
    public static InventoryItem* GetInventoryItem(ref this ItemOrderModuleSorter sorter, long slotIndex) {
        if (sorter.Items.LongCount <= slotIndex) return null;

        var item = sorter.Items[slotIndex].Value;
        if (item == null) return null;

        var container = InventoryManager.Instance()->GetInventoryContainer(sorter.InventoryType + item->Page);
        if (container == null) return null;

        return container->GetInventorySlot(item->Slot);
    }
}
