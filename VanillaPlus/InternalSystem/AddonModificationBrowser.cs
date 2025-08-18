using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Extensions;
using VanillaPlus.Utilities;

namespace VanillaPlus.InternalSystem;

public class AddonModificationBrowser : NativeAddon {

    private ResNode mainContainerNode = null!;
    
    private HorizontalFlexNode searchContainerNode = null!;
    private TextInputNode searchBoxNode = null!;
    private TextNode searchLabelNode = null!;
    private ScrollingAreaNode<TreeListNode> optionContainerNode = null!;
    private ResNode descriptionContainerNode = null!;
    private ImGuiImageNode descriptionImageNode = null!;
    private BorderNineGridNode borderNineGridNode = null!;
    private TextNode descriptionImageTextNode = null!;
    private TextNode descriptionTextNode = null!;
    private TextNode descriptionVersionTextNode = null!;
    private TextButtonNode changelogButtonNode = null!;

    private const float ItemPadding = 5.0f;

    private GameModificationOptionNode? selectedOption;
    
    private readonly AddonChangelogBrowser? changelogBrowser = new() {
        InternalName = "VPChangelog",
        Title = "Vanilla Plus Changelog Browser",
        NativeController = System.NativeController,
        Size = new Vector2(450.0f, 400.0f),
    };

    private readonly List<TreeListCategoryNode> categoryNodes = [];
    private readonly List<GameModificationOptionNode> modificationOptionNodes = [];

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        categoryNodes.Clear();
        modificationOptionNodes.Clear();
        
        mainContainerNode = new ResNode {
            Position = ContentStartPosition,
            Size = ContentSize,
            IsVisible = true,
        };
        AttachNode(mainContainerNode);

        BuildOptionsContainer();
        BuildSearchContainer();
        BuildDescriptionContainer();
        
        var groupedOptions = System.ModificationManager.LoadedModifications
                                   .Select(option => option)
                                   .GroupBy(option => option.Modification.ModificationInfo.Type)
                                   .OrderBy(option => option.Key);

        foreach (var category in groupedOptions) {
            var newCategoryNode = new TreeListCategoryNode {
                IsVisible = true,
                Label = category.Key.GetDescription(),
                OnToggle = isVisible => OnCategoryToggled(isVisible, category.Key),
            };

            foreach (var mod in category.OrderBy(modification => modification.Modification.ModificationInfo.DisplayName)) {
                var newOptionNode = new GameModificationOptionNode {
                    Height = 42.0f,
                    Modification = mod,
                    IsVisible = true,
                };

                newOptionNode.OnClick = () => OnOptionClicked(newOptionNode);

                newCategoryNode.AddNode(newOptionNode);
                modificationOptionNodes.Add(newOptionNode);
            }
            
            categoryNodes.Add(newCategoryNode);
            optionContainerNode.ContentNode.AddCategoryNode(newCategoryNode);
        }

        RecalculateScrollableAreaSize();
        UpdateSizes();
    }
    
    private void BuildOptionsContainer() {
        optionContainerNode = new ScrollingAreaNode<TreeListNode> {
            IsVisible = true,
            ContentHeight = 1000.0f,
            ScrollSpeed = 24,
        };
        System.NativeController.AttachNode(optionContainerNode, mainContainerNode);
    }

    private void BuildSearchContainer() {
        searchContainerNode = new HorizontalFlexNode {
            Height = 28.0f,
            AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
            IsVisible = true,
        };
        System.NativeController.AttachNode(searchContainerNode, mainContainerNode);
        
        searchBoxNode = new TextInputNode {
            IsVisible = true,
            OnInputReceived = OnSearchBoxInputReceived,
        };
        searchContainerNode.AddNode(searchBoxNode);

        searchLabelNode = new TextNode {
            Position = new Vector2(10.0f, 6.0f),
            IsVisible = true,
            TextColor = ColorHelper.GetColor(3),
            Text = "Search . . .",
        };
        System.NativeController.AttachNode(searchLabelNode, searchBoxNode);

        searchBoxNode.OnFocused += () => {
            searchLabelNode.IsVisible = false;
        };

        searchBoxNode.OnUnfocused += () => {
            if (searchBoxNode.String.ToString() is "") {
                searchLabelNode.IsVisible = true;
            }
        };
    }

    private void BuildDescriptionContainer() {
        descriptionContainerNode = new ResNode {
            IsVisible = true,
        };
        System.NativeController.AttachNode(descriptionContainerNode, mainContainerNode);
        
        descriptionTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
            FontSize = 14,
            LineSpacing = 22,
            FontType = FontType.Axis,
            IsVisible = true,
            Text = "Please select an option on the left",
            TextColor = ColorHelper.GetColor(1),
        };
        System.NativeController.AttachNode(descriptionTextNode, descriptionContainerNode);
        
        descriptionImageTextNode = new TextNode {
            AlignmentType = AlignmentType.TopLeft,
            TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
            FontSize = 14,
            LineSpacing = 22,
            FontType = FontType.Axis,
            IsVisible = true,
            TextColor = ColorHelper.GetColor(1),
        };
        System.NativeController.AttachNode(descriptionImageTextNode, descriptionContainerNode);

        changelogButtonNode = new TextButtonNode {
            Label = "Changelog",
            OnClick = OnChangelogButtonClicked,
        };
        System.NativeController.AttachNode(changelogButtonNode, descriptionContainerNode);
        
        descriptionVersionTextNode = new TextNode {
            IsVisible = true,
            AlignmentType = AlignmentType.BottomRight,
            TextColor = ColorHelper.GetColor(3),
        };
        System.NativeController.AttachNode(descriptionVersionTextNode, descriptionContainerNode);
                        
        descriptionImageNode = new ImGuiImageNode {
            WrapMode = 2,
            ImageNodeFlags = 0,
            EventFlagsSet = true,
        };
        
        descriptionImageNode.AddEvent(AddonEventType.MouseClick, _ => {
            if (descriptionImageNode.Scale == Vector2.One * 2.5f) {
                descriptionImageNode.Scale = Vector2.One;
            }
            else {
                descriptionImageNode.Scale = new Vector2(2.5f, 2.5f);
            }
        });
        System.NativeController.AttachNode(descriptionImageNode, descriptionContainerNode);
        
        borderNineGridNode = new BorderNineGridNode {
            IsVisible = true,
        };
        System.NativeController.AttachNode(borderNineGridNode, descriptionImageNode);
    }

    private void OnCategoryToggled(bool isVisible, ModificationType type) {
        var selectionCategory = selectedOption?.Modification.Modification.ModificationInfo.Type;
        if (selectionCategory is not null) {
            if (!isVisible && selectionCategory == type) {
                ClearSelection();
            }
        }

        RecalculateScrollableAreaSize();
    }
    
    private void OnSearchBoxInputReceived(SeString searchTerm) {
        List<GameModificationOptionNode> validOptions = [];
        
        foreach (var option in modificationOptionNodes) {
            var isTarget = option.ModificationInfo.IsMatch(searchTerm.ToString());
            option.IsVisible = isTarget;

            if (isTarget) {
                validOptions.Add(option);
            }
        }

        foreach (var categoryNode in categoryNodes) {
            categoryNode.IsVisible = validOptions.Any(option => option.ModificationInfo.Type.GetDescription() == categoryNode.Label.ToString());
            categoryNode.RecalculateLayout();
        }

        if (validOptions.All(option => option != selectedOption)) {
            ClearSelection();
        }
        
        optionContainerNode.ContentNode.RefreshLayout();
        RecalculateScrollableAreaSize();
    }

    private void OnOptionClicked(GameModificationOptionNode option) {
        ClearSelection();
        
        selectedOption = option;
        selectedOption.IsSelected = true;

        if (selectedOption.Modification.Modification.ImageName is { } assetName) {
            Task.Run(() => LoadModuleImage(assetName));
            
            descriptionImageNode.IsVisible = true;
            descriptionImageTextNode.IsVisible = true;
            descriptionTextNode.IsVisible = false;
            descriptionImageTextNode.Text = selectedOption.Modification.Modification.ModificationInfo.Description;
        }
        else {
            descriptionImageNode.IsVisible = false;
            descriptionImageTextNode.IsVisible = false;
            descriptionTextNode.IsVisible = true;
            descriptionTextNode.Text = selectedOption.Modification.Modification.ModificationInfo.Description;
        }

        changelogButtonNode.IsVisible = true;
        descriptionVersionTextNode.IsVisible = true;
        descriptionVersionTextNode.Text = $"Version {selectedOption.Modification.Modification.ModificationInfo.Version}";
    }

    private async void LoadModuleImage(string assetName) {
        try {
            var assetPath = Assets.GetAssetPath(assetName);
            var texture = await Services.TextureProvider.GetFromFile(assetPath).RentAsync();
            Assets.LoadedTextures.Add(texture);
            descriptionImageNode.LoadTexture(texture);
            descriptionImageNode.TextureSize = texture.Size;
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Exception while loading Module Image");
        }
    }

    private void ClearSelection() {
        selectedOption = null;
        foreach (var node in modificationOptionNodes) {
            node.IsSelected = false;
            node.IsHovered = false;
        }

        descriptionTextNode.IsVisible = true;
        descriptionTextNode.Text = "Please select an option on the left";

        descriptionImageNode.Scale = Vector2.One;
        
        descriptionImageNode.IsVisible = false;
        descriptionImageTextNode.IsVisible = false;
        descriptionVersionTextNode.IsVisible = false;
        changelogButtonNode.IsVisible = false;
    }

    private void OnChangelogButtonClicked() {
        if (changelogBrowser is not null && selectedOption is not null) {
            if (changelogBrowser.IsOpen) {
                changelogBrowser.Close();
            }

            if (System.SystemConfig.ChangelogWindowPosition is { } changelogPosition) {
                changelogBrowser.Position = changelogPosition;
            }
            
            changelogBrowser.Modification = selectedOption.Modification.Modification;
            changelogBrowser.Title = $"{selectedOption.ModificationInfo.DisplayName} Changelog";
            changelogBrowser.Open();
        }
    }

    private void RecalculateScrollableAreaSize() {
        optionContainerNode.ContentHeight = categoryNodes.Sum(node => node.Height) + 5.0f;
    }

    public void UpdateDisabledState() {
        if (changelogBrowser?.IsOpen ?? false) {
            foreach (var modificationOptionNode in modificationOptionNodes) {
                modificationOptionNode.UpdateDisabledState();
            }
        }
    }

    private void UpdateSizes() {
        searchContainerNode.Size = new Vector2(mainContainerNode.Width, 28.0f);
        optionContainerNode.Position = new Vector2(0.0f, searchContainerNode.Height + ItemPadding);
        optionContainerNode.Size = new Vector2(mainContainerNode.Width * 3.0f / 5.0f - ItemPadding, mainContainerNode.Height - searchContainerNode.Height - ItemPadding);
        descriptionContainerNode.Position = new Vector2(mainContainerNode.Width * 3.0f / 5.0f, searchContainerNode.Height + ItemPadding);
        descriptionContainerNode.Size = new Vector2(mainContainerNode.Width * 2.0f / 5.0f, mainContainerNode.Height - searchContainerNode.Height - ItemPadding);

        descriptionImageNode.Size = new Vector2(descriptionContainerNode.Width * 0.66f, descriptionContainerNode.Width * 0.66f);
        descriptionImageNode.Origin = descriptionImageNode.Size / 2.0f;
        descriptionImageNode.Position = new Vector2(descriptionContainerNode.Width * 0.33f / 2.0f, descriptionContainerNode.Width * 0.33f / 4.0f);

        changelogButtonNode.Size = new Vector2(125.0f, 28.0f);
        changelogButtonNode.Position = new Vector2(0.0f, descriptionContainerNode.Height - changelogButtonNode.Height - ItemPadding);
        
        descriptionVersionTextNode.Size = new Vector2(200.0f, 28.0f);
        descriptionVersionTextNode.Position = descriptionContainerNode.Size - descriptionVersionTextNode.Size - new Vector2(8.0f, 8.0f);

        descriptionImageTextNode.Size = new Vector2(descriptionContainerNode.Width - 16.0f, descriptionContainerNode.Height - descriptionImageNode.Y - descriptionImageNode.Height - descriptionVersionTextNode.Height - 14.0f);
        descriptionImageTextNode.Position = new Vector2(8.0f, descriptionImageNode.Position.Y + descriptionImageNode.Height + 8.0f);

        borderNineGridNode.Size = descriptionImageNode.Size + new Vector2(30.0f, 30.0f);
        borderNineGridNode.Position = new Vector2(-15.0f, -15.0f);
        
        descriptionTextNode.Size = descriptionContainerNode.Size - new Vector2(16.0f, 16.0f) - new Vector2(0.0f, descriptionVersionTextNode.Height);
        descriptionTextNode.Position = new Vector2(8.0f, 8.0f);
        
        foreach (var node in categoryNodes) {
            node.Width = optionContainerNode.ContentNode.Width;
        }
    }
    
    protected override unsafe void OnHide(AtkUnitBase* addon) {
        System.SystemConfig.BrowserWindowPosition = Position;
        System.SystemConfig.Save();
    }
}
