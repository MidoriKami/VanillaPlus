using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.CurrencyWarning.Nodes;

public class CurrencyWarningSettingListItemNode : ListItemWithFocusNav<CurrencyWarningSetting>, IListItemNode {

    /// <inheritdoc/>
    public static float ItemHeight => 48.0f;

    /// <inheritdoc/>
    protected override void SetNodeData(CurrencyWarningSetting itemData) {
        if (!Services.GetService<IDataManager>().GetExcelSheet<Item>().TryGetRow(itemData.ItemId, out var item)) return;

        iconNode.IconId = item.Icon;
        labelTextNode.String = item.Name.ToString();
    }

    public CurrencyWarningSettingListItemNode() {
        iconNode = new IconImageNode {
            FitTexture = true,
        };
        iconNode.AttachNode(this);

        labelTextNode = new TextNode {
            TextFlags = TextFlags.Ellipsis | TextFlags.Emboss,
        };
        labelTextNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        iconNode.Size = new Vector2(Height - 4.0f, Height - 4.0f);
        iconNode.Position = new Vector2(2.0f, 2.0f);

        labelTextNode.Size = new Vector2(Width - iconNode.Width - 4.0f, Height);
        labelTextNode.Position = new Vector2(iconNode.Bounds.Right + 2.0f, 0.0f);
    }

    private readonly IconImageNode iconNode;
    private readonly TextNode labelTextNode;
}
