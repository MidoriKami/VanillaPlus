using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Components.Search;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using Lumina.Data.Parsing.Uld;
using Lumina.Extensions;
using VanillaPlus.Features.GearsetRedirect.Config;

namespace VanillaPlus.Features.GearsetRedirect.Addons;

/// <summary>
/// Window used to create a new <see cref="RedirectionConfig"/>
/// </summary>
public class NewRedirectionAddon : NativeAddon {

    /// <summary>
    /// Action that is invoked when the selection is confirmed.
    /// </summary>
    public Action<RedirectionConfig>? OnSelectionConfirmed { get; set; }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        new VerticalListNode {
            Position = ContentStartPosition,
            Size = new Vector2(ContentSize.X, ContentSize.Y - 26.0f),
            FitWidth = true,
            FirstItemSpacing = 24.0f,
            ItemSpacing = 8.0f,
            InitialNodes = [
                new HorizontalFlexNode {
                    Height = 26.0f,
                    AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
                    InitialNodes = [
                        new TextNode {
                            Height = 26.0f,
                            String = "Switch to Gearset",
                        },
                        gearsetButtonNode = new TextButtonNode {
                            Height = 26.0f,
                            String = "Select target Gearset",
                            OnClick = OnGearsetButtonClicked,
                        },
                    ],
                },
                new HorizontalFlexNode {
                    Height = 26.0f,
                    AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
                    InitialNodes = [
                        new TextNode {
                            Height = 26.0f,
                            String = "When in Zone",
                        },
                        territoryButtonNode = new TextButtonNode {
                            Height = 26.0f,
                            String = "Select target Zone",
                            OnClick = OnTerritoryButtonClicked,
                        },
                    ],
                },
            ],
        }.AttachNode(this);

        new HorizontalFlexNode {
            Position = ContentStartPosition + new Vector2(0.0f, ContentSize.Y - 26.0f),
            Size = new Vector2(ContentSize.X, 26.0f),
            AlignmentFlags = FlexFlags.FitWidth | FlexFlags.FitHeight,
            InitialNodes = [
                new TextButtonNode {
                    TextId = 1, // "OK"
                    SheetType = NodeData.SheetType.Addon,
                    OnClick = OnConfirmClicked,
                },
                new TextButtonNode {
                    TextId = 2, // "Cancel"
                    SheetType = NodeData.SheetType.Addon,
                    OnClick = OnCancelClicked,
                },
            ],
        }.AttachNode(this);
    }



    private void OnConfirmClicked() {
        if (newRedirectionConfig is { TerritoryType: not 0, AlternateGearsetId: not 0 }) {
            OnSelectionConfirmed?.Invoke(newRedirectionConfig);
        }
        Close();
    }

    private void OnCancelClicked() {
        Close();
    }

    private void OnGearsetButtonClicked() {
        gearsetSearchAddon.ConfirmedSelections = results => {
            if (results.FirstOrNull() is { } result) {
                newRedirectionConfig.AlternateGearsetId = result.Id;
                gearsetButtonNode?.String = result.NameString;
            }
        };

        gearsetSearchAddon.Open();
    }

    private void OnTerritoryButtonClicked() {
        territorySearchAddon.ConfirmedSelections = results => {
            if (results.FirstOrNull() is { } result) {
                newRedirectionConfig.TerritoryType = result.RowId;
                territoryButtonNode?.String = result.PlaceName.ValueNullable?.Name.ToString() ?? string.Empty;
            }
        };

        territorySearchAddon.Open();
    }

    public override async ValueTask DisposeAsync() {
        await Task.WhenAll(
            gearsetSearchAddon.DisposeAsync().AsTask(),
            territorySearchAddon.DisposeAsync().AsTask()
        );

        await base.DisposeAsync();
    }

    private readonly RedirectionConfig newRedirectionConfig = new();
    private TextButtonNode? gearsetButtonNode;
    private TextButtonNode? territoryButtonNode;

    private readonly GearsetSearchAddon gearsetSearchAddon = new() {
        Size = new Vector2(275.0f, 535.0f),
        InternalName = "GearsetSearch",
        Title = Strings.SearchAddon_GearsetTitle,
        AllowMultiselect = false,
    };

    private readonly TerritoryTypeSearchAddon territorySearchAddon = new() {
        Size = new Vector2(450.0f, 530.0f),
        InternalName = "TerritorySearch",
        Title = Strings.SearchAddon_TerritoryTitle,
        OptionsList = Services.DataManager.GetTerritoryTypes().ToList(),
        AllowMultiselect = false,
    };
}
