using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using VanillaPlus.NativeElements.Addons;

namespace VanillaPlus.Features.RetrieveAllMateriaFromGearPiece;

public class MateriaRetrievalProgressAddon(
    Queue<QueuedGearPiece> queuedGearItems,
    List<GearPieceNodeData> finishedGearItems
)
    : NodeListAddon<GearPieceNodeData, GearPieceListItemNode> {
    protected override unsafe void OnUpdate(AtkUnitBase* addon) {
        base.OnUpdate(addon);

        ListNode?.DisableCollisionNode = true;
        ListNode?.ShowClickableCursor = false;
        ListNode?.OptionsList = finishedGearItems.Concat(
                queuedGearItems.Select(
                    // Need to convert it to a basic type and cannot use QueuedItem,
                    // because then equal check does not work from NodeList and the values are never updated.
                    item => item.ToGearListItemNodeData()
                )
            )
            .ToList();
    }
}
