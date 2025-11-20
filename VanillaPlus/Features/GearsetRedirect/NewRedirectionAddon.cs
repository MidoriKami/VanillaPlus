using System.Linq;
using System.Numerics;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Addons;
using KamiToolKit.Premade.Nodes;
using Lumina.Excel.Sheets;
using VanillaPlus.NativeElements.Addons.SearchAddons;
using Action = System.Action;

namespace VanillaPlus.Features.GearsetRedirect;

public class NewRedirectionAddon : NativeAddon {

    private TextNode? gearsetLabelNode;
    private SearchInfoNode<GearsetInfo>? gearsetInfoNode;
    private TextButtonNode? selectGearsetButtonNode;

    private VerticalLineNode? verticalLineNode;

    private TextNode? zoneLabelNode;
    private LuminaSearchInfoNode<TerritoryType>? zoneInfoNode;
    private TextButtonNode? selectZoneButtonNode;

    private HorizontalLineNode? horizontalLineNode;

    private TextButtonNode? confirmButtonNode;
    private TextButtonNode? cancelButtonNode;

    private readonly SearchAddon<GearsetInfo> gearsetSearchAddon = GearsetSearchAddon.GetAddon();
    private readonly LuminaSearchAddon<TerritoryType> territorySearchAddon = TerritorySearchAddon.GetAddon();

    public GearsetInfo? SelectedGearset { get; private set; }
    public TerritoryType? SelectedTerritory { get; private set; }
    
    public override void Dispose() {
        base.Dispose();

        gearsetSearchAddon.Dispose();
        territorySearchAddon.Dispose();
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        SelectedGearset = null;
        SelectedTerritory = null;
        
        var halfWidth = ContentSize.X / 2.0f - 8.0f;
        
        gearsetLabelNode = new TextNode {
            Size = new Vector2(halfWidth, 32.0f),
            Position = ContentStartPosition + new Vector2(0.0f, 6.0f),
            AlignmentType = AlignmentType.Center,
            FontSize = 24,
            String = "Gearset",
        };
        gearsetLabelNode.AttachNode(this);

        gearsetInfoNode = new SearchInfoNode<GearsetInfo> {
            Size = new Vector2(halfWidth, 64.0f),
            Position = new Vector2(ContentStartPosition.X, gearsetLabelNode.Y + gearsetLabelNode.Height + 16.0f),
            Option = new GearsetInfo {
                GearsetId = -1,
            },
        };
        gearsetInfoNode.AttachNode(this);

        selectGearsetButtonNode = new TextButtonNode {
            Size = new Vector2(halfWidth * 2.0f / 3.0f, 32.0f),
            Position = new Vector2(ContentStartPosition.X + halfWidth / 2.0f - new Vector2(halfWidth * 2.0f / 3.0f, 32.0f).X / 2.0f, gearsetInfoNode.Y + gearsetInfoNode.Height + 4.0f),
            String = "Select Gearset",
            OnClick = OnSelectGearset,
        };
        selectGearsetButtonNode.AttachNode(this);

        verticalLineNode = new VerticalLineNode {
            Size = new Vector2(8.0f, ContentSize.Y - 36.0f),
            Position = new Vector2(gearsetLabelNode.X + gearsetLabelNode.Width + 12.0f, ContentStartPosition.Y),
        };
        verticalLineNode.AttachNode(this);

        horizontalLineNode = new HorizontalLineNode {
            Size = new Vector2(ContentSize.X, 8.0f),
            Position = ContentStartPosition + new Vector2(0.0f, ContentSize.Y - 32.0f - 5.0f),
        };
        horizontalLineNode.AttachNode(this);

        zoneLabelNode = new TextNode {
            Size = new Vector2(halfWidth, 32.0f),
            Position = ContentStartPosition + new Vector2(ContentSize.X - halfWidth, 6.0f),
            FontSize = 24,
            AlignmentType = AlignmentType.Center,
            String = "Zone",
        };
        zoneLabelNode.AttachNode(this);

        zoneInfoNode = new LuminaSearchInfoNode<TerritoryType> {
            Size = new Vector2(halfWidth, 64.0f),
            Position = new Vector2(zoneLabelNode.X, zoneLabelNode.Y + zoneLabelNode.Height + 16.0f),
            GetLabelFunc = territory => territory.RowId is 1 ? "Nothing Selected" : territory.PlaceName.Value.Name.ToString(),
            GetSubLabelFunc = territory => territory.ContentFinderCondition.RowId is 0 ? string.Empty : territory.ContentFinderCondition.Value.Name.ToString(),
            GetIconIdFunc = _ => 60072,
            GetTexturePathFunc = territory => territory.LoadingImage.Value.FileName.ToString().IsNullOrEmpty() ? string.Empty : $"ui/loadingimage/{territory.LoadingImage.Value.FileName}_hr1.tex",
            Option = Services.DataManager.GetExcelSheet<TerritoryType>().First(),
        };
        zoneInfoNode.AttachNode(this);

        selectZoneButtonNode = new TextButtonNode {
            Size = new Vector2(halfWidth * 2.0f / 3.0f, 32.0f),
            Position = new Vector2(zoneLabelNode.X + halfWidth / 2.0f - new Vector2(halfWidth * 2.0f / 3.0f, 32.0f).X / 2.0f, zoneInfoNode.Y + zoneInfoNode.Height + 4.0f),
            String = "Select Zone",
            OnClick = OnSelectZone,
        };
        selectZoneButtonNode.AttachNode(this);

        confirmButtonNode = new TextButtonNode {
            Size = new Vector2(100.0f, 24.0f),
            Position = ContentStartPosition + new Vector2(0.0f, ContentSize.Y - 24.0f),
            String = "Confirm",
            OnClick = OnConfirm,
        };
        confirmButtonNode.AttachNode(this);

        cancelButtonNode = new TextButtonNode {
            Size = new Vector2(100.0f, 24.0f),
            Position = ContentStartPosition + new Vector2(ContentSize.X - 100.0f, ContentSize.Y - 24.0f),
            String = "Cancel",
            OnClick = OnCancel,
        };
        cancelButtonNode.AttachNode(this);
    }

    private void OnCancel() {
        Close();
    }

    private void OnConfirm() {
        if (selectGearsetButtonNode is not null && selectZoneButtonNode is not null) {
            OnSelectionsConfirmed?.Invoke();
        } 

        Close();
    }

    private void OnSelectGearset() {
        gearsetSearchAddon.UpdateGearsets();
        gearsetSearchAddon.SelectionResult = result => {
            SelectedGearset = result;

            if (gearsetInfoNode is not null) {
                gearsetInfoNode.Option = result;
            }
        };
        
        gearsetSearchAddon.Open();
    }

    private void OnSelectZone() {
        territorySearchAddon.SelectionResult = result => {
            SelectedTerritory = result;

            if (zoneInfoNode is not null) {
                zoneInfoNode.Option = result;
            }
        };

        territorySearchAddon.Open();
    }

    public Action? OnSelectionsConfirmed { get; set; }
}
