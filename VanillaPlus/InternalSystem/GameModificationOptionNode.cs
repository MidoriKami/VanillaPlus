using System.Numerics;
using Dalamud.Game.Addon.Events;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using Action = System.Action;
using Addon = VanillaPlus.Utilities.Addon;

namespace VanillaPlus.InternalSystem;

public class GameModificationOptionNode : SimpleComponentNode {

    private readonly NineGridNode hoveredBackgroundNode;
    private readonly NineGridNode selectedBackgroundNode;
    private readonly CheckboxNode checkboxNode;
    private readonly IconImageNode erroringImageNode;
    private readonly TextNode modificationNameNode;
    private readonly IconImageNode experimentalImageNode;
    private readonly TextNode authorNamesNode;
    private readonly CircleButtonNode configButtonNode;

    public GameModificationOptionNode() {
        hoveredBackgroundNode = new SimpleNineGridNode {
            TexturePath = "ui/uld/ListItemA.tex",
            TextureCoordinates = new Vector2(0.0f, 22.0f),
            TextureSize = new Vector2(64.0f, 22.0f),
            TopOffset = 6,
            BottomOffset = 6,
            LeftOffset = 16,
            RightOffset = 1,
            IsVisible = false,
        };
        System.NativeController.AttachNode(hoveredBackgroundNode, this);
        
        selectedBackgroundNode = new SimpleNineGridNode {
            TexturePath = "ui/uld/ListItemA.tex",
            TextureCoordinates = new Vector2(0.0f, 0.0f),
            TextureSize = new Vector2(64.0f, 22.0f),
            TopOffset = 6,
            BottomOffset = 6,
            LeftOffset = 16,
            RightOffset = 1,
            IsVisible = false,
        };
        System.NativeController.AttachNode(selectedBackgroundNode, this);
        
        checkboxNode = new CheckboxNode {
            Origin = new Vector2(8.0f, 8.0f),
            Scale = new Vector2(2.0f, 2.0f),
            IsVisible = true,
            OnClick = ToggleModification,
        };
        System.NativeController.AttachNode(checkboxNode, this);

        erroringImageNode = new IconImageNode {
            Scale = new Vector2(2.0f, 2.0f),
            Origin = new Vector2(8.0f, 8.0f),
            IconId = 61502,
            ImageNodeFlags = 0,
            WrapMode = 2,
            Tooltip = "Failed to load, this module has been disabled",
        };
        System.NativeController.AttachNode(erroringImageNode, this);

        modificationNameNode = new TextNode {
            IsVisible = true,
            TextFlags = TextFlags.AutoAdjustNodeSize | TextFlags.Ellipsis,
            AlignmentType = AlignmentType.BottomLeft,
            TextColor = ColorHelper.GetColor(1),
        };
        System.NativeController.AttachNode(modificationNameNode, this);

        experimentalImageNode = new IconImageNode {
            IconId = 60073,
            Tooltip = "Caution, this feature is experimental.\nMay contain bugs or crash your game.",
        };
        System.NativeController.AttachNode(experimentalImageNode, this);
        
        authorNamesNode = new TextNode {
            IsVisible = true,
            FontType = FontType.Axis,
            TextFlags = TextFlags.AutoAdjustNodeSize | TextFlags.Ellipsis,
            AlignmentType = AlignmentType.TopLeft,
            TextColor = ColorHelper.GetColor(3),
        };
        System.NativeController.AttachNode(authorNamesNode, this);

        configButtonNode = new CircleButtonNode {
            Icon = ButtonIcon.GearCog,
            Tooltip = "Open configuration window",
        };
        System.NativeController.AttachNode(configButtonNode, this);
        
        CollisionNode.AddEvent(AddonEventType.MouseOver, _ => {
            if (!IsSelected) {
                IsHovered = true;
            }
        });
        
        CollisionNode.AddEvent(AddonEventType.MouseDown, _ => {
            OnClick?.Invoke();
        });
        
        CollisionNode.AddEvent(AddonEventType.MouseOut, _ => {
            IsHovered = false;
        });
    }

    public ModificationInfo ModificationInfo => Modification.Modification.ModificationInfo;
    
    public required LoadedModification Modification {
        get;
        set {
            field = value;
            modificationNameNode.Text = value.Modification.ModificationInfo.DisplayName;
            authorNamesNode.Text = $"By {string.Join(", ", value.Modification.ModificationInfo.Authors)}";

            RefreshConfigWindowButton();

            checkboxNode.IsChecked = value.State is LoadedState.Enabled;

            experimentalImageNode.IsVisible = value.Modification.IsExperimental;
            experimentalImageNode.EnableEventFlags = value.Modification.IsExperimental;

            UpdateDisabledState();
        }
    }
    
    private void ToggleModification(bool shouldEnableModification) {
        if (shouldEnableModification && Modification.State is LoadedState.Disabled) {
            System.ModificationManager.TryEnableModification(Modification);
        }
        else if (!shouldEnableModification && Modification.State is LoadedState.Enabled) {
            System.ModificationManager.TryDisableModification(Modification);
        }

        UpdateDisabledState();
        
        OnClick?.Invoke();
        RefreshConfigWindowButton();
    }

    public Action? OnClick { get; set; }
    
    public bool IsHovered {
        get => hoveredBackgroundNode.IsVisible;
        set => hoveredBackgroundNode.IsVisible = value;
    }
    
    public bool IsSelected {
        get => selectedBackgroundNode.IsVisible;
        set {
            selectedBackgroundNode.IsVisible = value;
            hoveredBackgroundNode.IsVisible = !value;
        }
    }

    private void RefreshConfigWindowButton() {
        if (Modification.Modification.OpenConfigAction is not null) {
            configButtonNode.IsVisible = true;
            configButtonNode.OnClick = () => {
                Modification.Modification.OpenConfigAction();
                OnClick?.Invoke();
            };

            configButtonNode.IsEnabled = Modification.State is LoadedState.Enabled;
        }
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        hoveredBackgroundNode.Size = Size;
        selectedBackgroundNode.Size = Size;

        checkboxNode.Size = new Vector2(16.0f, 16.0f);
        checkboxNode.Position = new Vector2(Height / 4.0f + checkboxNode.Width / 2.0f, Height / 2.0f - checkboxNode.Height / 2.0f - 4.0f);

        modificationNameNode.Height = Height / 2.0f;
        modificationNameNode.Position = new Vector2(Height + Height / 3.0f, 0.0f);
        
        experimentalImageNode.Size = new Vector2(16.0f, 16.0f);
        experimentalImageNode.Position = new Vector2(modificationNameNode.X, modificationNameNode.Height);
        
        authorNamesNode.Height = Height / 2.0f;
        authorNamesNode.Position = new Vector2(Height * 2.0f, Height / 2.0f);

        configButtonNode.Size = new Vector2(Height * 2.0f / 3.0f, Height * 2.0f / 3.0f);
        configButtonNode.Position = new Vector2(Width - Height, Height / 2.0f - configButtonNode.Height / 2.0f);
        
        erroringImageNode.Size = checkboxNode.Size - new Vector2(4.0f, 4.0f);
        erroringImageNode.Position = checkboxNode.Position + new Vector2(5.0f, 8.0f);
    }

    public void UpdateDisabledState() {
        if (Modification.State is LoadedState.Errored or LoadedState.CompatError) {
            checkboxNode.IsEnabled = false;
            erroringImageNode.IsVisible = true;
            erroringImageNode.EnableEventFlags = true;
            erroringImageNode.Tooltip = Modification.ErrorMessage;

        }
        else {
            checkboxNode.IsEnabled = true;
            erroringImageNode.IsVisible = false;
            erroringImageNode.EnableEventFlags = false;
        }

        Addon.UpdateCollisionForNode(this);

        checkboxNode.IsChecked = Modification.State is LoadedState.Enabled;
        configButtonNode.IsEnabled = Modification.State is LoadedState.Enabled;
    }
}
