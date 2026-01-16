using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.ListItemNodes;
using KamiToolKit.Premade.SearchAddons;
using Lumina.Excel.Sheets;
using VanillaPlus.Features.GearsetRedirect.Nodes;
using VanillaPlus.NativeElements.SearchAddons;
using Action = System.Action;

namespace VanillaPlus.Features.GearsetRedirect;

public class NewRedirectionAddon : NativeAddon {
    private GearsetInfoListItemNode? gearsetInfoNode;
    private TerritoryTypeListItemNode? zoneInfoNode;

    private readonly GearsetSearchAddon gearsetSearchAddon = new() {
        Size = new Vector2(275.0f, 555.0f),
        InternalName = "GearsetSearch",
        Title = Strings.SearchAddon_GearsetTitle,
    };

    private readonly TerritorySearchAddon? territorySearchAddon = new() {
        Size = new Vector2(400.0f, 735.0f),
        InternalName = "TerritorySearch",
        Title = Strings.SearchAddon_TerritoryTitle,
    };

    public GearsetInfo? SelectedGearset { get; private set; }
    public TerritoryType? SelectedTerritory { get; private set; }
    public Action? OnSelectionsConfirmed { get; set; }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        SelectedGearset = null;
        SelectedTerritory = null;

        AddNode(new VerticalListNode {
            Size = ContentSize,
            Position = ContentStartPosition,
            FitWidth = true,
            ItemSpacing = 2.0f,
            InitialNodes = [
                new HorizontalListNode {
                    Height = ContentSize.Y - 36.0f,
                    FitHeight = true,
                    InitialNodes = [
                        new VerticalListNode {
                            Width = ContentSize.X * 3.0f / 7.0f - 8.0f,
                            FitWidth = true,
                            InitialNodes = [
                                new TextNode {
                                    Height = 48.0f,
                                    FontSize = 24,
                                    String = "Gearset",
                                    AlignmentType = AlignmentType.Center,
                                },
                                new ResNode { Height = 16.0f },
                                gearsetInfoNode = new GearsetInfoListItemNode {
                                    EnableSelection = false,
                                    EnableHighlight = false,
                                    Height = 64.0f,
                                },
                                new ResNode { Height = 24.0f },
                                new TextButtonNode {
                                    Height = 28.0f,
                                    String = "Select Gearset",
                                    OnClick = () => {
                                        gearsetSearchAddon.SelectionResult = result => {
                                            SelectedGearset = new GearsetInfo {
                                                GearsetId = result.Id,
                                            };
        
                                            gearsetInfoNode?.ItemData = SelectedGearset;
                                        };
        
                                        gearsetSearchAddon.Open();
                                    },
                                },
                            ],
                        },
                        new ResNode { Width = 8.0f },
                        new VerticalLineNode{ Width = 2.0f },
                        new VerticalListNode {
                            Width = ContentSize.X * 4.0f / 7.0f,
                            FitWidth = true,
                            InitialNodes = [
                                new TextNode {
                                    Height = 48.0f,
                                    FontSize = 24,
                                    String = "Zone",
                                    AlignmentType = AlignmentType.Center,
                                },
                                new ResNode { Height = 16.0f },
                                zoneInfoNode = new TerritoryTypeListItemNode {
                                    EnableSelection = false,
                                    EnableHighlight = false,
                                    Height = 64.0f,
                                },
                                new ResNode { Height = 24.0f },
                                new TextButtonNode {
                                    Height = 28.0f,
                                    String = "Select Zone",
                                    OnClick = () => {
                                        territorySearchAddon?.SelectionResult = result => {
                                            SelectedTerritory = result;
        
                                            zoneInfoNode?.ItemData = result;
                                        };
        
                                        territorySearchAddon?.Open();
                                    },
                                },
                            ],
                        },
                    ],
                },
                new HorizontalLineNode { Height = 4.0f },
                new HorizontalListNode {
                    Height = 28.0f,
                    Alignment = HorizontalListAnchor.Right,
                    FitHeight = true,
                    ItemSpacing = 4.0f,
                    InitialNodes = [
                        new TextButtonNode {
                            Width = 100.0f,
                            String = "Confirm",
                            OnClick = () => {
                                if (SelectedGearset is not null && SelectedTerritory is not null) {
                                    OnSelectionsConfirmed?.Invoke();
                                } 

                                Close();
                            },
                        },
                        new TextButtonNode {
                            Width = 100.0f,
                            String = "Cancel",
                            OnClick = Close,
                        },
                    ],
                },
            ],
        });
    }

    public override void Dispose() {
        base.Dispose();

        gearsetSearchAddon.Dispose();
        territorySearchAddon?.Dispose();
    }
}
