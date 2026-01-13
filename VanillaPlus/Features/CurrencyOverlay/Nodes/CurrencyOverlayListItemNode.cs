using KamiToolKit.Premade.GenericSearchListItemNodes;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.CurrencyOverlay.Nodes;

public class CurrencyOverlayListItemNode : GenericCurrencyListItemNode<CurrencySetting> {
    protected override void SetNodeData(CurrencySetting itemData) {
        var item = Services.DataManager.GetExcelSheet<Item>().GetRow(itemData.ItemId);
        
        IconNode.IconId = item.Icon;
        LabelTextNode.String = item.Name.ToString();
    }
}
