using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;

namespace VanillaPlus.Extensions;

public static unsafe class NodeBaseExtensions {
    extension(NodeBase node) {
        public void ShowInventoryItemTooltip(InventoryType container, short slot)
            => AtkStage.Instance()->ShowInventoryItemTooltip(node, container, slot);
    }
}
