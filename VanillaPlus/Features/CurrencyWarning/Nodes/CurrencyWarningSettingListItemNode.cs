using Lumina.Excel.Sheets;
using VanillaPlus.Native.Nodes;

namespace VanillaPlus.Features.CurrencyWarning.Nodes;

public class CurrencyWarningSettingListItemNode : SimpleListItemNode<CurrencyWarningSetting> {
    protected override void SetNodeData(CurrencyWarningSetting itemData) {
        if (!Services.DataManager.GetExcelSheet<Item>().TryGetRow(itemData.ItemId, out var item)) return;

        IconNode.IconId = item.Icon;
        LabelTextNode.String = item.Name.ToString();
    }
}
