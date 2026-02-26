using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPiece;

public class MateriaRetrievalProgressAddon(Queue<QueuedItem> queuedItems, List<QueuedItemNodeData> finishedItems)
    : NodeListAddon<QueuedItemNodeData, QueuedItemNode> {
    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        ListNode?.DisableCollisionNode = true;
        ListNode?.ShowClickableCursor = false;
        ListNode?.OptionsList = finishedItems.Concat(
                queuedItems.Select(
                    // Need to convert it to a basic type and cannot use QueuedItem,
                    // because then equal check does not work from NodeList and the values are never updated.
                    item => item.ToQueuedItemNodeData()
                )
            )
            .ToList();
    }
}
