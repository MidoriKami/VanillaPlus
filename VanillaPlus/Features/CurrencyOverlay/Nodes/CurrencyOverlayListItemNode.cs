using KamiToolKit.Premade.GenericListItemNodes;

namespace VanillaPlus.Features.CurrencyOverlay.Nodes;

public class CurrencyOverlayListItemNode : GenericSimpleListItemNode<CurrencySetting> {
    protected override void SetNodeData(CurrencySetting itemData) {
        var item = Services.DataManager.GetItem(itemData.ItemId);
        
        IconNode.IconId = item.Icon;
        LabelTextNode.String = item.Name.ToString();
    }
}
