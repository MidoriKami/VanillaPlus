using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Game.ClientState.Aetherytes;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node;
using KamiToolKit.Premade.Node.Simple;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.BetterTeleportWindow;

public class TeleportAddon : NativeAddon {

    private TextInputNode? textInputNode;

    private SimpleImageNode? mapBackgroundNode;
    private SimpleImageNode? mapPreviewNode;
    private TextNineGridNode? mapLabelNode;
    private CircleButtonNode? ticketConfigButton;

    private ListMode currentMode = ListMode.All;
    private uint currentRegionId;

    private ListNode<IAetheryteEntry, TeleportListItemNode>? listNode;
    private readonly Dictionary<ListMode, SelectableNode> premadeNodes = [];
    private readonly List<SelectableNode> selectableNodes = [];
    private readonly BetterTeleportWindowConfig config;

    public TeleportAddon(BetterTeleportWindowConfig config) {
        this.config = config;
        this.config.OnSave += OnConfigSaved;
    }

    public override void Dispose() {
        base.Dispose();

        config.OnSave -= OnConfigSaved;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        const float itemSpacing = 5.0f;

        SetWindowSize(new Vector2(600.0f, 689.0f));

        AddNode(new HorizontalListNode { // Main Container
            Size = ContentSize,
            Position = ContentStartPosition,
            FitHeight = true,
            FirstItemSpacing = itemSpacing,
            InitialNodes = [
                new VerticalListNode { // Premade options + Region Lists
                    Width = ContentSize.X * 2.0f / 5.0f - itemSpacing,
                    FitWidth = true,
                    ItemSpacing = itemSpacing,
                    InitialNodes = [
                        new VerticalListNode { // Premade Options
                            FitContents = true,
                            FitWidth = true,
                            InitialNodes = GetPremadeNodes(),
                        },
                        new HorizontalLineNode { Height = 2.0f },
                        new VerticalListNode { // Regions
                            FitContents = true,
                            FitWidth = true,
                            ItemSpacing = itemSpacing / 2.0f,
                            InitialNodes = GetRegionNodes(),
                        },
                    ],
                },
                new VerticalLineNode { Width = 2.0f },
                new VerticalListNode { // Searchbar + Results
                    Width = ContentSize.X * 3.0f / 5.0f - itemSpacing,
                    ItemSpacing = itemSpacing,
                    FitWidth = true,
                    InitialNodes = [
                        new HorizontalListNode { // Search Container
                            Height = 28.0f,
                            FitHeight = true,
                            InitialNodes = [
                                textInputNode = new TextInputNode { // Search
                                    Width = 317.0f,
                                    PlaceholderString = Strings.SearchPlaceholder,
                                    AutoSelectAll = true,
                                    OnInputReceived = OnSearchBoxInputReceived,
                                },
                                new CheckboxNode {
                                    Size = new Vector2(28.0f, 28.0f),
                                    IsChecked = config.AutoFocusSearch,
                                    TextTooltip = "Toggle Auto Focus Searchbar",
                                    OnClick = newValue => {
                                        config.AutoFocusSearch = newValue;
                                        config.Save();
                                    },
                                },
                            ],
                        },
                        listNode = new ListNode<IAetheryteEntry, TeleportListItemNode> { // Results
                            Height = ContentSize.Y - 28.0f - itemSpacing * 2.0f,
                            ItemSpacing = itemSpacing * 1.5f,
                            OptionsList = Services.AetheryteList.ToList(),
                            NoResultsString = "No Aetherytes Match Search",
                        },
                    ],
                },
            ],
        });

        mapBackgroundNode = new BackgroundImageNode {
            Size = new Vector2(350.0f, 350.0f),
            Position = new Vector2(Size.X + 24.0f, 0.0f),
            FitTexture = true,
            IsVisible = false,
            Color = new Vector4(231.0f, 219.0f, 181.0f, 255.0f) / 255.0f,
        };
        mapBackgroundNode.AttachNode(this);

        mapPreviewNode = new SimpleImageNode {
            Size = new Vector2(350.0f, 350.0f),
            Position = new Vector2(Size.X + 24.0f, 0.0f),
            TextureSize = new Vector2(2048.0f, 2048.0f),
            TextureCoordinates = new Vector2(0.0f, 0.0f),
            FitTexture = true,
            IsVisible = false,
        };
        mapPreviewNode.AttachNode(this);

        mapLabelNode = new TextNineGridNode {
            Size = new Vector2(350.0f, 32.0f),
            Position = mapPreviewNode.Position,
            IsVisible = false,
            AlignmentType = AlignmentType.Center,
            FontSize = 24,
        };
        mapLabelNode.AttachNode(this);

        ticketConfigButton = new CircleButtonNode {
            Size = new Vector2(28.0f, 28.0f),
            Position = new Vector2(ContentSize.X - 36.0f, 6.0f),
            Icon = ButtonIcon.GearCog,
            TextTooltip = Services.DataManager.GetAddonText(8515), // "Open Teleport Settings"
            OnClick = () => AgentTeleport.Instance()->AgentInterface.SendCommand(2, [3, 0, 0]),
        };
        ticketConfigButton.AttachNode(this);

        // Remember the last used category, and select it.
        if (premadeNodes.TryGetValue(config.LastListMode, out var selectableNode)) {
            OnPremadeOptionClicked(selectableNode, config.LastListMode);
        }

        if (config.AutoFocusSearch) {
            textInputNode.SetFocus();
        }
    }

    public unsafe void SetPreviewImage(IAetheryteEntry? entry) {
        if (entry is null) return;
        if (mapPreviewNode is null) return;

        mapBackgroundNode?.IsVisible = true;

        // This doesn't like having the old texture unloaded, or the path themed, so we will load it directly.
        mapPreviewNode.PartsList[0]->UldAsset->AtkTexture.LoadTexture(entry.MapTexturePath);
        mapPreviewNode.IsVisible = true;

        mapLabelNode?.String = entry.PlaceName;
        mapLabelNode?.IsVisible = true;
    }

    public void ClearPreviewImage() {
        mapBackgroundNode?.IsVisible = false;
        mapPreviewNode?.IsVisible = false;
        mapLabelNode?.IsVisible = false;
    }

    private List<NodeBase> GetPremadeNodes() {
        List<NodeBase> nodeList = [];

        foreach (var premadeOption in (List<ListMode>) [ ListMode.All, ListMode.Cities, ListMode.Favorites ]) {
            var newSelectableOption = new SelectableTextNode {
                Height = 24.0f,
                IsSelected = premadeOption is ListMode.All,
                String = premadeOption.Description,
                OnClick = node => OnPremadeOptionClicked(node, premadeOption),
            };
            premadeNodes.Add(premadeOption, newSelectableOption);
            selectableNodes.Add(newSelectableOption);
            nodeList.Add(newSelectableOption);
        }

        return nodeList;
    }

    private void OnPremadeOptionClicked(SelectableNode targetNode, ListMode premadeOption) {
        foreach (var node in selectableNodes) {
            node.IsSelected = false;
        }

        targetNode.IsSelected = true;
        currentMode = premadeOption;
        config.LastListMode = currentMode;
        config.Save();

        textInputNode?.String = string.Empty;
        OnSearchBoxInputReceived(string.Empty);
    }

    private List<NodeBase> GetRegionNodes() {
        var regionNodes = Services.AetheryteList
            .DistinctBy(entry => entry.RegionId)
            .Select(entry => new SelectableTextNode {
                Height = 24.0f,
                String = entry.RegionName,
                OnClick = thisNode => OnRegionEntryClicked(thisNode, entry.RegionId),
            })
            .ToList();

        selectableNodes.AddRange(regionNodes);

        return regionNodes.Cast<NodeBase>().ToList();
    }

    private void OnRegionEntryClicked(SelectableNode targetNode, uint regionId) {
        if (regionId is 0) return;

        foreach (var node in selectableNodes) {
            node.IsSelected = false;
        }

        targetNode.IsSelected = true;

        currentMode = ListMode.Region;
        currentRegionId = regionId;

        listNode?.ResetScroll();

        textInputNode?.String = string.Empty;
        OnSearchBoxInputReceived(string.Empty);
    }

    private void OnConfigSaved() {
        listNode?.ResetScroll();
        textInputNode?.String = string.Empty;
        OnSearchBoxInputReceived(string.Empty);
    }

    private void OnSearchBoxInputReceived(ReadOnlySeString searchString) {
        listNode?.OptionsList = currentMode switch {
            ListMode.All => Services.AetheryteList.Where(entry => IsMatch(entry, searchString)).ToList(),
            ListMode.Region => Services.AetheryteList.Where(entry => entry.RegionId == currentRegionId && IsMatch(entry, searchString)).ToList(),
            ListMode.Cities => Services.AetheryteList.Where(entry => entry.AetheryteData.ValueNullable?.AethernetGroup is not 0 && IsMatch(entry, searchString)).ToList(),
            ListMode.Favorites => Services.AetheryteList.Where(entry => config.FavoriteAetherytes.Contains(entry.AetheryteId) && IsMatch(entry, searchString)).ToList(),
            _ => Services.AetheryteList.Where(entry => IsMatch(entry, searchString)).ToList(),
        };

        if (listNode?.OptionsList.Count > 0) {
            SetPreviewImage(listNode?.OptionsList.FirstOrDefault());
        }
        else {
            ClearPreviewImage();
        }
    }

    protected override unsafe void OnHide(AtkUnitBase* addon) {
        base.OnHide(addon);

        AgentTeleport.Instance()->Hide();
    }

    private bool IsMatch(IAetheryteEntry entry, ReadOnlySeString searchString) {
        var regex = new Regex(searchString.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (regex.IsMatch(entry.AetheryteName.ToString())) return true;
        if (regex.IsMatch(entry.PlaceName.ToString())) return true;

        if (config.CustomNames.TryGetValue(entry.AetheryteId, out var customName) && regex.IsMatch(customName)) return true;

        return false;
    }
}
