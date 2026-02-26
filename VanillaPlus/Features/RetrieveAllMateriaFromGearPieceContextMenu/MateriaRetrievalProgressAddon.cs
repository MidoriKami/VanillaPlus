using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPieceContextMenu;

// todo pass current queue and past items separately
public class MateriaRetrievalProgressAddon(List<QueuedItem> fullListOfQueuedItems)
    : NodeListAddon<QueuedItemNodeData, QueuedItemNode> {
    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        ListNode?.DisableCollisionNode = true;
        ListNode?.ShowClickableCursor = false;
        ListNode?.OptionsList = fullListOfQueuedItems.Select(
                // Need to convert it to a basic type and cannot use QueuedItem,
                // because then equal check does not work from NodeList and the values are never updated.
                (item => new QueuedItemNodeData {
                        StartingMateriaCount = item.StartingPointMateriaCount,
                        CurrentMateriaCount = item.CurrentMateriaCount,
                        ItemId = item.GetItemId(),
                        Status = item.LastRetrievalAttemptStatus,
                    })
            )
            .ToList();
    }
}
