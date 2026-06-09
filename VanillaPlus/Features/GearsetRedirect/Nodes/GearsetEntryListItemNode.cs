using System.Numerics;
using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.GearsetRedirect.Nodes;

/// <summary>
/// Node representing a gearset option.
/// </summary>
public class GearsetEntryListItemNode : ListItemWithFocusNav<GearsetRedirectionEntry>, IListItemNode {

    /// <inheritdoc/>
    public static float ItemHeight => 48.0f;

    /// <summary>
    /// Gets the icon node used to display this item.
    /// </summary>
    protected IconImageNode ItemIconImageNode { get; }

    /// <summary>
    /// Gets the text node used to display the items name.
    /// </summary>
    protected TextNode GearsetNameTextNode { get; }

    /// <summary>
    /// Gets the text node used to display the items ItemUiCategory.
    /// </summary>
    protected TextNode GearsetItemLevelTextNode { get; }

    /// <summary>
    /// Gets the text node used to display the items ID.
    /// </summary>
    protected TextNode GearsetIndexTextNode { get; }

    /// <inheritdoc/>
    protected override unsafe void SetNodeData(GearsetRedirectionEntry itemData) {
        var gearsetInfo = RaptureGearsetModule.Instance()->GetGearset(itemData.TargetGearsetId);
        if (gearsetInfo is null) {
            Services.PluginLog.Warning("Attempted to populate null gearset entry.", "GearsetRedirect");
            return;
        }

        ItemIconImageNode.IconId = (uint) (gearsetInfo->ClassJob + 62000);
        ItemIconImageNode.IsVisible = true;

        GearsetNameTextNode.String = gearsetInfo->NameString;
        GearsetNameTextNode.IsVisible = true;

        GearsetItemLevelTextNode.String = $"{SeIconChar.ItemLevel.ToIconString()}{gearsetInfo->ItemLevel}";
        GearsetItemLevelTextNode.IsVisible = true;

        GearsetIndexTextNode.String = $"{gearsetInfo->Id + 1}";
        GearsetIndexTextNode.IsVisible = true;
    }

    public GearsetEntryListItemNode() {
        ItemIconImageNode = new IconImageNode {
            FitTexture = true,
        };
        ItemIconImageNode.AttachNode(this);

        GearsetNameTextNode = new TextNode {
            TextFlags = TextFlags.Ellipsis | TextFlags.Emboss,
            FontSize = 14,
            AlignmentType = AlignmentType.BottomLeft,
        };
        GearsetNameTextNode.AttachNode(this);

        GearsetItemLevelTextNode = new TextNode {
            TextFlags = TextFlags.Ellipsis | TextFlags.Emboss,
            FontSize = 12,
            AlignmentType = AlignmentType.TopLeft,
            TextColor = ColorHelper.GetColor(3),
            TextOutlineColor = ColorHelper.GetColor(7),
        };
        GearsetItemLevelTextNode.AttachNode(this);

        GearsetIndexTextNode = new TextNode {
            TextFlags = TextFlags.Emboss,
            FontSize = 10,
            AlignmentType = AlignmentType.BottomRight,
            TextColor = ColorHelper.GetColor(3),
        };
        GearsetIndexTextNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        ItemIconImageNode.Size = new Vector2(Height - 4.0f, Height - 4.0f);
        ItemIconImageNode.Position = new Vector2(2.0f, 2.0f);

        GearsetIndexTextNode.Size = new Vector2(48.0f, Height / 2.0f);
        GearsetIndexTextNode.Position = new Vector2(Width - GearsetIndexTextNode.Width, 0.0f);

        GearsetNameTextNode.Size = new Vector2(Width - ItemIconImageNode.Width - GearsetIndexTextNode.Width - 8.0f, Height / 2.0f);
        GearsetNameTextNode.Position = new Vector2(ItemIconImageNode.Bounds.Right + 4.0f, 0.0f);

        GearsetItemLevelTextNode.Size = GearsetNameTextNode.Size;
        GearsetItemLevelTextNode.Position = GearsetNameTextNode.Position with { Y = Height / 2.0f };
    }
}
