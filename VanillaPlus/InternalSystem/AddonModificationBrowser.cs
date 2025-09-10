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
using VanillaPlus.Utilities;

namespace VanillaPlus.InternalSystem;

public class AddonModificationBrowser : NativeAddon {

    private ResNode mainContainerNode = null!;
    
    private HorizontalFlexNode searchContainerNode = null!;
    private TextInputNode searchBoxNode = null!;
    private ScrollingAreaNode<TreeListNode> optionContainerNode = null!;
    private ResNode descriptionContainerNode = null!;
    private ResNode descriptionImageFrame = null!;
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

    private bool isImageEnlarged;
    private bool isImageHovered;

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

        addon->AdditionalFocusableNodes[0] = (AtkResNode*)descriptionImageNode.Node;
        
        var groupedOptions = System.ModificationManager.LoadedModifications
                                   .Select(option => option)
                                   .GroupBy(option => option.Modification.ModificationInfo.Type)
                                   .OrderBy(option => option.Key);

        uint optionIndex = 0;
        
        foreach (var category in groupedOptions) {
            var newCategoryNode = new TreeListCategoryNode {
                IsVisible = true,
                SeString = category.Key.GetDescription(),
                OnToggle = isVisible => OnCategoryToggled(isVisible, category.Key),
            };

            foreach (var mod in category.OrderBy(modification => modification.Modification.ModificationInfo.DisplayName)) {
                var newOptionNode = new GameModificationOptionNode {
                    NodeId = optionIndex++,
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
            PlaceholderString = "Search . . .",
        };
        searchContainerNode.AddNode(searchBoxNode);
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
            String = "Please select an option on the left",
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
            SeString = "Changelog",
            OnClick = OnChangelogButtonClicked,
        };
        System.NativeController.AttachNode(changelogButtonNode, descriptionContainerNode);
        
        descriptionVersionTextNode = new TextNode {
            IsVisible = true,
            AlignmentType = AlignmentType.BottomRight,
            TextColor = ColorHelper.GetColor(3),
        };
        System.NativeController.AttachNode(descriptionVersionTextNode, descriptionContainerNode);

        descriptionImageFrame = new ResNode {
            IsVisible = true,
        };
        System.NativeController.AttachNode(descriptionImageFrame, descriptionContainerNode);

        descriptionImageNode = new ImGuiImageNode {
            EventFlagsSet = true,
            IsVisible = true,
            FitTexture = true,
        };

        descriptionImageNode.AddEvent(AddonEventType.MouseClick, _ => {
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

        descriptionImageNode.AddEvent(AddonEventType.MouseOver, _ => {
            if (isImageEnlarged) return;

            descriptionImageNode.Scale = new Vector2(1.05f, 1.05f);
            isImageHovered = true;
        });
        
        descriptionImageNode.AddEvent(AddonEventType.MouseOut, _ => {
            if (isImageEnlarged) return;
            
            descriptionImageNode.Scale = Vector2.One;
            isImageHovered = false;
        });
        System.NativeController.AttachNode(descriptionImageNode, descriptionImageFrame);
        
        
        borderNineGridNode = new BorderNineGridNode {
            IsVisible = true,
            Alpha = 125,
            Offsets = new Vector4(40.0f),
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
            categoryNode.IsVisible = validOptions.Any(option => option.ModificationInfo.Type.GetDescription() == categoryNode.SeString.ToString());
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
        descriptionVersionTextNode.String = $"Version {selectedOption.Modification.Modification.ModificationInfo.Version}";
    }

    private async void LoadModuleImage(string assetName) {
        try {
            var assetPath = Assets.GetAssetPath(assetName);
            var texture = await Services.TextureProvider.GetFromFile(assetPath).RentAsync();
            Assets.LoadedTextures.Add(texture);
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
        foreach (var node in modificationOptionNodes) {
            node.IsSelected = false;
            node.IsHovered = false;
        }

        descriptionTextNode.IsVisible = true;
        descriptionTextNode.String = "Please select an option on the left";

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
            changelogBrowser.Title = $"{selectedOption.ModificationInfo.DisplayName} Changelog";
            changelogBrowser.Open();
        }
    }

    private void RecalculateScrollableAreaSize() {
        optionContainerNode.ContentHeight = categoryNodes.Sum(node => node.Height) + 10.0f;
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

        descriptionImageFrame.Size = new Vector2(descriptionContainerNode.Width * 0.8f, descriptionContainerNode.Width * 0.8f);
        descriptionImageFrame.Position = new Vector2(descriptionContainerNode.Width * 0.2f / 2.0f, descriptionContainerNode.Width * 0.2f / 4.0f);

        changelogButtonNode.Size = new Vector2(125.0f, 28.0f);
        changelogButtonNode.Position = new Vector2(0.0f, descriptionContainerNode.Height - changelogButtonNode.Height - ItemPadding);
        
        descriptionVersionTextNode.Size = new Vector2(200.0f, 28.0f);
        descriptionVersionTextNode.Position = descriptionContainerNode.Size - descriptionVersionTextNode.Size - new Vector2(8.0f, 8.0f);

        descriptionImageTextNode.Size = new Vector2(descriptionContainerNode.Width - 16.0f, descriptionContainerNode.Height - descriptionImageFrame.Y - descriptionImageFrame.Height - descriptionVersionTextNode.Height - 14.0f);
        descriptionImageTextNode.Position = new Vector2(8.0f, descriptionImageFrame.Position.Y + descriptionImageFrame.Height + 8.0f);
        
        descriptionTextNode.Size = descriptionContainerNode.Size - new Vector2(16.0f, 16.0f) - new Vector2(0.0f, descriptionVersionTextNode.Height);
        descriptionTextNode.Position = new Vector2(8.0f, 8.0f);
        
        foreach (var node in categoryNodes) {
            node.Width = optionContainerNode.ContentNode.Width;
        }
    }
}
