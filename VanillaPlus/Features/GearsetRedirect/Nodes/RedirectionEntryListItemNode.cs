using System.Numerics;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.Features.GearsetRedirect.Config;

namespace VanillaPlus.Features.GearsetRedirect.Nodes;

/// <summary>
/// List item node for representing a specific <see cref="RedirectionConfig"/> within a specific gearsets redirection list.
/// For use with <see cref="GearsetRedirect"/>.
/// </summary>
public class RedirectionEntryListItemNode : ListItemWithFocusNav<RedirectionConfig>, IListItemNode {

    /// <inheritdoc/>
    public static float ItemHeight => 48.0f;

    /// <inheritdoc/>
    protected override unsafe void SetNodeData(RedirectionConfig itemData) {
        var targetGearset = RaptureGearsetModule.Instance()->GetGearset(itemData.AlternateGearsetId);
        if (targetGearset is null) {
            gearsetIconNode.IsVisible = false;
            gearsetNameTextNode.String = "Invalid Gearset Target";
            return;
        }

        gearsetIconNode.IconId = (uint) (62000 + targetGearset->ClassJob);
        gearsetIconNode.IsVisible = true;

        gearsetNameTextNode.String = targetGearset->Name;

        var territoryInfo = Services.GetService<IDataManager>().GetExcelSheet<TerritoryType>().GetRow(itemData.TerritoryType);
        territoryNameTextNode.String = $"When in {SeIconChar.ArrowRight.ToIconString()} {territoryInfo.PlaceName.ValueNullable?.Name.ToString() ?? string.Empty}";
    }

    public RedirectionEntryListItemNode() {
        gearsetIconNode = new IconImageNode {
            FitTexture = true,
        };
        gearsetIconNode.AttachNode(this);

        gearsetNameTextNode = new TextNode {
            AlignmentType = AlignmentType.BottomLeft,
        };
        gearsetNameTextNode.AttachNode(this);

        territoryNameTextNode = new TextNode {
            AlignmentType = AlignmentType.TopLeft,
            TextColor = ColorHelper.GetColor(3),
        };
        territoryNameTextNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        gearsetIconNode.Size = new Vector2(Height, Height);
        gearsetIconNode.Position = new Vector2(0.0f, 0.0f);

        gearsetNameTextNode.Size = new Vector2(Width, Height / 2.0f);
        gearsetNameTextNode.Position = new Vector2(gearsetIconNode.Bounds.Right, 0.0f);

        territoryNameTextNode.Size = new Vector2(Width, Height / 2.0f);
        territoryNameTextNode.Position = new Vector2(gearsetIconNode.Bounds.Right, Height / 2.0f);
    }

    private readonly IconImageNode gearsetIconNode;
    private readonly TextNode gearsetNameTextNode;
    private readonly TextNode territoryNameTextNode;
}
