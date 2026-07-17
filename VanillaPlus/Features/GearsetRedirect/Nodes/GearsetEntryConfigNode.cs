  using System;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Components.ConfigurationNodes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using Lumina.Data.Parsing.Uld;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using VanillaPlus.Features.GearsetRedirect.Config;

namespace VanillaPlus.Features.GearsetRedirect.Nodes;

/// <summary>
/// The main body node for configuring a specific gearset, and configuring its sub-redirections.
/// </summary>
public class GearsetEntryConfigNode : EntryConfigurationNode<GearsetRedirectionEntry> {

    /// <inheritdoc/>
    protected override unsafe void PopulateEntryData(GearsetRedirectionEntry entry) {
        redirectionEntry = entry;

        var gearsetInfo = RaptureGearsetModule.Instance()->GetGearset(entry.TargetGearsetId);
        if (gearsetInfo == null) return;

        gearsetNameTextNode.String = gearsetInfo->NameString;
        backgroundImageNode.IconId = (uint) (62000 + gearsetInfo->ClassJob);

        redirectionsListNode.OptionsList = entry.Redirections;
    }

    public GearsetEntryConfigNode() {
        backgroundImageNode = new IconImageNode {
            FitTexture = true,
             Alpha = 0.1f,
        };
        backgroundImageNode.AttachNode(ConfigurationContentNode);

        layoutContainerNode = new VerticalListNode {
            FirstItemSpacing = 8.0f,
            ItemSpacing = 4.0f,
            FitWidth = true,
            InitialNodes = [
                gearsetNameTextNode = new TextNode {
                    Height = 64.0f,
                    AlignmentType = AlignmentType.Center,
                    TextFlags = TextFlags.Ellipsis,
                    FontSize = 28,

                    // Force visible for LayoutRecalc to set the node height.
                    IsVisible = true,
                },
                new SearchInputNode {
                    Height = 26.0f,
                    OnInputReceived = OnSearchInputChanged,
                },
                redirectionsListNode = new ListNode<RedirectionConfig, RedirectionEntryListItemNode> {
                    Height = 325.0f,
                    ItemSpacing = 4.0f,
                    OptionsList = [],
                    OnItemSelected = OnListItemSelected,
                },
            ],
        };
        layoutContainerNode.AttachNode(ConfigurationContentNode);

        buttonLayoutNode = new HorizontalFlexNode {
            Height = 26.0f,
            AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
            InitialNodes = [
                new TextButtonNode {
                    TextId = 302, // "Add"
                    SheetType = NodeData.SheetType.Addon,
                    OnClick = OnAddClicked,
                },
                removeButtonNode = new TextButtonNode {
                    TextId = 85, // "Remove"
                    SheetType = NodeData.SheetType.Addon,
                    OnClick = OnRemoveClicked,
                    IsEnabled = false,
                },
            ],
        };
        buttonLayoutNode.AttachNode(ConfigurationContentNode);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        var backgroundImageSize = Size * 3.0f / 4.0f;
        var minSize = MathF.Min(backgroundImageSize.X, backgroundImageSize.Y);
        var adjustedSize = new Vector2(minSize, minSize);

        backgroundImageNode.Size = adjustedSize;
        backgroundImageNode.Position = Size / 2.0f - backgroundImageNode.Size / 2.0f;

        layoutContainerNode.Size = new Vector2(Width, Height - 26.0f);
        layoutContainerNode.RecalculateLayout();

        buttonLayoutNode.Size = new Vector2(Width, 26.0f);
        buttonLayoutNode.Position = new Vector2(0.0f, layoutContainerNode.Bounds.Bottom);
        buttonLayoutNode.RecalculateLayout();
    }

    private void OnListItemSelected(RedirectionConfig? selectedOption) {
        selectedConfigOption = selectedOption;
        removeButtonNode.IsEnabled = selectedConfigOption is not null;
    }

    private unsafe void OnSearchInputChanged(ReadOnlySeString searchString) {
        var regex = searchString.AsRegex();

        redirectionsListNode.OptionsList = redirectionEntry?.Redirections.Where(entry => {
            var targetGearset = RaptureGearsetModule.Instance()->GetGearset(entry.AlternateGearsetId);
            if (targetGearset is null) return false;

            if (regex.IsMatch(targetGearset->NameString)) return true;

            var territory = IDataManager.Get().GetExcelSheet<TerritoryType>().GetRow(entry.TerritoryType);
            var territoryName = territory.PlaceName.ValueNullable?.Name.ToString();
            if (territoryName is not null && regex.IsMatch(territoryName)) return true;

            return false;
        }).ToList() ?? [];
    }

    private void OnAddClicked() {
        GearsetRedirect.CreateRedirectionAddon?.OnSelectionConfirmed = newRedirection => {
            if (newRedirection is { AlternateGearsetId: not 0, TerritoryType: not 0 }) {
                redirectionEntry?.Redirections.Add(newRedirection);
                redirectionsListNode.OptionsList = redirectionEntry?.Redirections ?? [];
                SaveConfig?.Invoke();
            }
        };

        GearsetRedirect.CreateRedirectionAddon?.Open();
    }

    private void OnRemoveClicked() {
        if (selectedConfigOption is null) return;
        if (redirectionEntry is null) return;

        redirectionEntry.Redirections.Remove(selectedConfigOption);
        redirectionsListNode.OptionsList = redirectionEntry.Redirections;
        SaveConfig?.Invoke();
    }

    private readonly IconImageNode backgroundImageNode;
    private readonly LayoutListNode layoutContainerNode;
    private readonly HorizontalFlexNode buttonLayoutNode;
    private readonly TextNode gearsetNameTextNode;
    private readonly TextButtonNode removeButtonNode;
    private readonly ListNode<RedirectionConfig, RedirectionEntryListItemNode> redirectionsListNode;

    private RedirectionConfig? selectedConfigOption;
    private GearsetRedirectionEntry? redirectionEntry;
}
