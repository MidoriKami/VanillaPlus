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
                queuedGearItems.Select(item => item.ToGearListItemNodeData())
            )
            .ToList();
    }
}
