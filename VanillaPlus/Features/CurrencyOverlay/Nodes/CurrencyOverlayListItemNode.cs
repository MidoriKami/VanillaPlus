using KamiToolKit.Premade.GenericSearchListItemNodes;

namespace VanillaPlus.Features.CurrencyOverlay.Nodes;

public class CurrencyOverlayListItemNode : GenericCurrencyListItemNode<CurrencySetting> {
    protected override void SetNodeData(CurrencySetting itemData) {
        var item = Services.DataManager.GetItem(itemData.ItemId);
        
        IconNode.IconId = item.Icon;
        LabelTextNode.String = item.Name.ToString();
    }
}
