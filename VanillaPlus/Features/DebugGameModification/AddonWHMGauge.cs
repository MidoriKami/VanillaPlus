using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.DebugGameModification;

public class AddonWhmGauge : NativeAddon {

    private TextNode? timeTextNode;
    
    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        addon->SubscribeAtkArrayData(1, (int)NumberArrayType.JobHud);

        timeTextNode = new SimpleLabelNode {
            Position = ContentStartPosition,
            Text = "number here",
        };
        AttachNode(timeTextNode);
    }

    protected override unsafe void OnRequestedUpdate(AtkUnitBase* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData) {
        timeTextNode?.Text = numberArrayData[(int)NumberArrayType.JobHud]->IntArray[4].ToString();
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        addon->UnsubscribeAtkArrayData(1, (int)NumberArrayType.JobHud);
    }
}
