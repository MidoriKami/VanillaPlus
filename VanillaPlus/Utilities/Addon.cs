using FFXIVClientStructs.FFXIV.Client.UI;
using KamiToolKit.BaseTypes;

namespace VanillaPlus.Utilities;

public static unsafe class Addon {
    public static void UpdateCollisionForNode(NodeBase node) {
        Services.Framework.RunOnFrameworkThread(() => {
            var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(node);
            if (addon is not null) {
                addon->UpdateCollisionNodeList(false);
            }
        });
    }
}
