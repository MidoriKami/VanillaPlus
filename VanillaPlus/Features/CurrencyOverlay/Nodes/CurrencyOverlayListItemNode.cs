using KamiToolKit.Premade.Node.ListItem;

namespace VanillaPlus.Features.CurrencyOverlay.Nodes;

public class CurrencyOverlayListItemNode : SimpleListItemNode<CurrencySetting> {
    protected override void SetNodeData(CurrencySetting itemData) {
        var item = Services.DataManager.GetItem(itemData.ItemId);
        
        IconNode.IconId = item.Icon;
        LabelTextNode.String = item.Name.ToString();
    }
}
