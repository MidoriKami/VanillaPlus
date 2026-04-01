using KamiToolKit.Premade.Node.ListItem;

namespace VanillaPlus.Features.WindowBackground.Nodes;

public class WindowBackgroundSettingListItemNode : SimpleListItemNode<WindowBackgroundSetting> {
    protected override void SetNodeData(WindowBackgroundSetting itemData) {
        IconNode.IconId = itemData.AddonName == WindowBackgroundSetting.InvalidName ? (uint) 5 : 61483;
        LabelTextNode.String = itemData.AddonName;
    }
}
