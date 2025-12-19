using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace VanillaPlus.Extensions;

public static unsafe class ItemOrderModuleSorterExtensions {
    extension(ref ItemOrderModuleSorter sorter) {
        public long GetSlotIndex(ItemOrderModuleSorterItemEntry* entry)
            => entry->Slot + sorter.ItemsPerPage * entry->Page;

        public InventoryItem* GetInventoryItem(ItemOrderModuleSorterItemEntry* entry)
            => sorter.GetInventoryItem(sorter.GetSlotIndex(entry));

        public InventoryItem* GetInventoryItem(long slotIndex) {
            if (sorter.Items.LongCount <= slotIndex) return null;

            var item = sorter.Items[slotIndex].Value;
            if (item == null) return null;

            var container = InventoryManager.Instance()->GetInventoryContainer(sorter.InventoryType + item->Page);
            if (container == null) return null;

            return container->GetInventorySlot(item->Slot);
        }
    }
}
