using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using Lumina.Text.ReadOnly;
using VanillaPlus.Enums;
using VanillaPlus.Utilities;

namespace VanillaPlus.InternalSystem;

public class AddonModificationBrowser : NativeAddon {

    private SimpleComponentNode mainContainerNode = null!;
    
    private HorizontalFlexNode searchContainerNode = null!;
    private TextInputNode searchBoxNode = null!;
    private ScrollingAreaNode<TreeListNode> optionContainerNode = null!;
    private SimpleComponentNode descriptionContainerNode = null!;
    private SimpleComponentNode descriptionImageFrame = null!;
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
        Title = Strings.ChangelogBrowserTitle,
        Size = new Vector2(450.0f, 400.0f),
    };

    private bool isImageEnlarged;
    private bool isImageHovered;

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        mainContainerNode = new SimpleComponentNode {
            Position = ContentStartPosition,
            Size = ContentSize,
        };
        mainContainerNode.AttachNode(this);

        BuildOptionsContainer();
        BuildSearchContainer();
        BuildDescriptionContainer();

        addon->AdditionalFocusableNodes[0] = (AtkResNode*)descriptionImageNode;

        uint optionIndex = 0;

        foreach (var category in PluginSystem.ModificationManager.CategoryGroups) {
            var newCategoryNode = new TreeListCategoryNode {
                SeString = category.Key.Description,
                OnToggle = isVisible => OnCategoryToggled(isVisible, category.Key),
                VerticalPadding = 0.0f,
            };

            foreach (var subCategory in PluginSystem.ModificationManager.SubCategoryGroups[category.Key]) {
                if (subCategory.Key is not null) {
                    var newHeaderNode = new TreeListHeaderNode {
                        Size = new Vector2(0.0f, 24.0f), 
                        SeString = subCategory.Key.Description, 
                    };
                    
                    newCategoryNode.AddNode(newHeaderNode);
                }

                foreach (var mod in subCategory.OrderBy(modification => modification.Modification.ModificationInfo.DisplayName)) {
                    newCategoryNode.AddNode(new GameModificationOptionNode {
                        NodeId = optionIndex++,
                        Height = 38.0f,
                        Modification = mod,
                        IsVisible = true,
                        OnClick = thisNode => OnOptionClicked((GameModificationOptionNode) thisNode),
                    });
                }
            }
            
            optionContainerNode.ContentNode.AddCategoryNode(newCategoryNode);
        }

        RecalculateScrollableAreaSize();
        UpdateSizes();
        
        OnSearchBoxInputReceived(PluginSystem.SystemConfig.CurrentSearch);
        searchBoxNode.SeString = PluginSystem.SystemConfig.CurrentSearch;
    }
    
    private void BuildOptionsContainer() {
        optionContainerNode = new ScrollingAreaNode<TreeListNode> {
            ContentHeight = 1000.0f,
            ScrollSpeed = 38,
        };
        optionContainerNode.AttachNode(mainContainerNode);
    }

    private void BuildSearchContainer() {
        searchContainerNode = new HorizontalFlexNode {
            Size = new Vector2(ContentSize.X, 28.0f),
            AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
        };
        searchContainerNode.AttachNode(mainContainerNode);
        
        searchBoxNode = new TextInputNode {
            PlaceholderString = Strings.SearchPlaceholder,
            AutoSelectAll = true,
            OnInputReceived = OnSearchBoxInputReceived,
            OnFocusLost = () => {
                PluginSystem.SystemConfig.CurrentSearch = searchBoxNode.String;
                PluginSystem.SystemConfig.Save();
            },
        };
        searchContainerNode.AddNode(searchBoxNode);
    }

    private void BuildDescriptionContainer() {
        descriptionContainerNode = new SimpleComponentNode();
        descriptionContainerNode.AttachNode(mainContainerNode);
        
        descriptionTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
            FontSize = 14,
            LineSpacing = 22,
            FontType = FontType.Axis,
            String = Strings.SelectionPrompt,
            TextColor = ColorHelper.GetColor(1),
        };
        descriptionTextNode.AttachNode(descriptionContainerNode);
        
        descriptionImageTextNode = new TextNode {
            AlignmentType = AlignmentType.TopLeft,
            TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
            FontSize = 14,
            LineSpacing = 22,
            FontType = FontType.Axis,
            TextColor = ColorHelper.GetColor(1),
        };
        descriptionImageTextNode.AttachNode(descriptionContainerNode);

        changelogButtonNode = new TextButtonNode {
            SeString = Strings.ChangelogButtonLabel,
            OnClick = OnChangelogButtonClicked,
            IsVisible = false,
        };
        changelogButtonNode.AttachNode(descriptionContainerNode);
        
        descriptionVersionTextNode = new TextNode {
            AlignmentType = AlignmentType.BottomRight,
            TextColor = ColorHelper.GetColor(3),
        };
        descriptionVersionTextNode.AttachNode(descriptionContainerNode);

        descriptionImageFrame = new SimpleComponentNode();
        descriptionImageFrame.AttachNode(descriptionContainerNode);

        descriptionImageNode = new ImGuiImageNode {
            FitTexture = true,
            ShowClickableCursor = true,
        };

        descriptionImageNode.AddEvent(AtkEventType.MouseClick, () => {
            if (!isImageEnlarged) {
                descriptionImageNode.Scale = new Vector2(2.5f, 2.5f);
            }
            else {
                if (isImageHovered) {
                    descriptionImageNode.Scale = new Vector2(1.05f, 1.05f);
                }
                else {
                    descriptionImageNode.Scale = Vector2.One;
                }
            }

            isImageEnlarged = !isImageEnlarged;
        });

        descriptionImageNode.AddEvent(AtkEventType.MouseOver, () => {
            if (isImageEnlarged) return;

            descriptionImageNode.Scale = new Vector2(1.05f, 1.05f);
            isImageHovered = true;
        });
        
        descriptionImageNode.AddEvent(AtkEventType.MouseOut, () => {
            if (isImageEnlarged) return;
            
            descriptionImageNode.Scale = Vector2.One;
            isImageHovered = false;
        });
        descriptionImageNode.AttachNode(descriptionImageFrame);
        
        borderNineGridNode = new BorderNineGridNode {
            Alpha = 125,
            Offsets = new Vector4(40.0f),
        };
        borderNineGridNode.AttachNode(descriptionImageNode);
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
    
    private void OnSearchBoxInputReceived(ReadOnlySeString searchTerm) {
        List<GameModificationOptionNode> validOptions = [];
        
        foreach (var option in optionContainerNode.ContentNode.CategoryNodes.SelectMany(category => category.GetNodes<GameModificationOptionNode>())) {
            var isTarget = option.ModificationInfo.IsMatch(searchTerm.ToString());
            option.IsVisible = isTarget;

            if (isTarget) {
                validOptions.Add(option);
            }
        }

        foreach (var headerNode in optionContainerNode.ContentNode.CategoryNodes.SelectMany(category => category.HeaderNodes)) {
            headerNode.IsVisible = searchTerm.ToString() == string.Empty;
        }

        foreach (var categoryNode in optionContainerNode.ContentNode.CategoryNodes) {
            categoryNode.IsVisible = validOptions.Any(option => option.ModificationInfo.Type.Description == categoryNode.SeString.ToString());
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
            
            descriptionImageFrame.IsVisible = true;
            descriptionImageTextNode.IsVisible = true;
            descriptionTextNode.IsVisible = false;
            descriptionImageTextNode.String = selectedOption.Modification.Modification.ModificationInfo.Description;
        }
        else {
            descriptionImageFrame.IsVisible = false;
            descriptionImageTextNode.IsVisible = false;
            descriptionTextNode.IsVisible = true;
            descriptionTextNode.String = selectedOption.Modification.Modification.ModificationInfo.Description;
        }

        changelogButtonNode.IsVisible = true;
        descriptionVersionTextNode.IsVisible = true;
        descriptionVersionTextNode.String = Strings.VersionLabelFormat.Format(
            selectedOption.Modification.Modification.ModificationInfo.Version);
    }

    private async void LoadModuleImage(string assetName) {
        try {
            var texture = await Services.TextureProvider.GetFromFile(Assets.GetAssetPath(assetName)).RentAsync();
            descriptionImageNode.LoadTexture(texture);
            descriptionImageNode.TextureSize = texture.Size;

            if (texture.Width > texture.Height) {
                var ratio = texture.Width / descriptionImageFrame.Width;
                var multiplier = 1 / ratio;

                descriptionImageNode.Width = descriptionImageFrame.Width;
                descriptionImageNode.Height = texture.Height * multiplier;
                descriptionImageNode.Y = (descriptionImageFrame.Width - descriptionImageNode.Height) / 2.0f;
                descriptionImageNode.X = 0.0f;
            }
            else {
                var ratio = texture.Height / descriptionImageFrame.Width;
                var multiplier = 1 / ratio;

                descriptionImageNode.Height = descriptionImageFrame.Width;
                descriptionImageNode.Width = texture.Width * multiplier;
                descriptionImageNode.X = (descriptionImageFrame.Width - descriptionImageNode.Width) / 2.0f;
                descriptionImageNode.Y = 0.0f;
            }

            descriptionImageNode.Origin = descriptionImageNode.Size / 2.0f;
            
            borderNineGridNode.Position = new Vector2(-16.0f, -16.0f);
            borderNineGridNode.Size = descriptionImageNode.Size + new Vector2(32.0f, 32.0f);
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Exception while loading Module Image");
        }
    }

    private void ClearSelection() {
        selectedOption = null;
        foreach (var node in optionContainerNode.ContentNode.CategoryNodes.SelectMany(category => category.GetNodes<GameModificationOptionNode>())) {
            node.IsSelected = false;
            node.IsHovered = false;
        }

        descriptionTextNode.IsVisible = true;
        descriptionTextNode.String = Strings.SelectionPrompt;

        descriptionImageFrame.Scale = Vector2.One;
        
        descriptionImageFrame.IsVisible = false;
        descriptionImageTextNode.IsVisible = false;
        descriptionVersionTextNode.IsVisible = false;
        changelogButtonNode.IsVisible = false;
    }

    private void OnChangelogButtonClicked() {
        if (changelogBrowser is not null && selectedOption is not null) {
            if (changelogBrowser.IsOpen) {
                changelogBrowser.Close();
            }

            changelogBrowser.Modification = selectedOption.Modification.Modification;
            changelogBrowser.Title = Strings.ChangelogTitleFormat.Format(selectedOption.ModificationInfo.DisplayName);
            changelogBrowser.Open();
        }
    }

    private void RecalculateScrollableAreaSize() {
        optionContainerNode.ContentHeight = optionContainerNode.ContentNode.CategoryNodes.Sum(node => node.Height) + 20.0f;
    }

    public void UpdateDisabledState() {
        if (IsOpen) {
            foreach (var modificationOptionNode in optionContainerNode.ContentNode.CategoryNodes.SelectMany(category => category.GetNodes<GameModificationOptionNode>())) {
                modificationOptionNode.UpdateDisabledState();
            }
        }
    }

    private void UpdateSizes() {
        searchContainerNode.Size = new Vector2(mainContainerNode.Width, 28.0f);

        optionContainerNode.Position = new Vector2(0.0f, searchContainerNode.Height + ItemPadding);
        optionContainerNode.Size = new Vector2(mainContainerNode.Width / 2.0f - ItemPadding, mainContainerNode.Height - searchContainerNode.Height - ItemPadding);

        descriptionContainerNode.Position = new Vector2(mainContainerNode.Width / 2.0f, searchContainerNode.Height + ItemPadding);
        descriptionContainerNode.Size = new Vector2(mainContainerNode.Width / 2.0f, mainContainerNode.Height - searchContainerNode.Height - ItemPadding);

        descriptionImageFrame.Size = new Vector2(descriptionContainerNode.Width * 0.8f, descriptionContainerNode.Width * 0.8f);
        descriptionImageFrame.Position = new Vector2(descriptionContainerNode.Width * 0.2f / 2.0f, descriptionContainerNode.Width * 0.2f / 4.0f);

        changelogButtonNode.Size = new Vector2(150.0f, 28.0f);
        changelogButtonNode.Position = new Vector2(0.0f, descriptionContainerNode.Height - changelogButtonNode.Height - ItemPadding);
        
        descriptionVersionTextNode.Size = new Vector2(200.0f, 28.0f);
        descriptionVersionTextNode.Position = descriptionContainerNode.Size - descriptionVersionTextNode.Size - new Vector2(8.0f, 8.0f);

        descriptionImageTextNode.Size = new Vector2(descriptionContainerNode.Width - 16.0f, descriptionContainerNode.Height - descriptionImageFrame.Y - descriptionImageFrame.Height - descriptionVersionTextNode.Height - 22.0f);
        descriptionImageTextNode.Position = new Vector2(8.0f, descriptionImageFrame.Position.Y + descriptionImageFrame.Height + 16.0f);
        
        descriptionTextNode.Size = descriptionContainerNode.Size - new Vector2(16.0f, 16.0f) - new Vector2(0.0f, descriptionVersionTextNode.Height);
        descriptionTextNode.Position = new Vector2(8.0f, 8.0f);
        
        foreach (var node in optionContainerNode.ContentNode.CategoryNodes) {
            node.Width = optionContainerNode.ContentNode.Width;
        }
    }
}
