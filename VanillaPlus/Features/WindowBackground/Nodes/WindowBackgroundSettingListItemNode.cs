using KamiToolKit.Premade.GenericListItemNodes;

namespace VanillaPlus.Features.WindowBackground.Nodes;

public class WindowBackgroundSettingListItemNode : GenericSimpleListItemNode<WindowBackgroundSetting> {
    protected override void SetNodeData(WindowBackgroundSetting itemData) {
        IconNode.IconId = itemData.AddonName == WindowBackgroundSetting.InvalidName ? (uint) 5 : 61483;
        LabelTextNode.String = itemData.AddonName;
    }
}
