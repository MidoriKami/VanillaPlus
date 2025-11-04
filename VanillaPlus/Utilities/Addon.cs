using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.System;

namespace VanillaPlus.Utilities;

public static unsafe class Addon {
    public static void UpdateCollisionForNode(NodeBase node) {
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(node);
        if (addon is not null) {
            addon->UpdateCollisionNodeList(false);
        }
    }
}
