using System.Numerics;
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
    private readonly CircleButtonNode reloadButtonNode;
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
        hoveredBackgroundNode.AttachNode(this);
        
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
        selectedBackgroundNode.AttachNode(this);
        
        checkboxNode = new CheckboxNode {
            OnClick = ToggleModification,
        };
        checkboxNode.AttachNode(this);

        erroringImageNode = new IconImageNode {
            IconId = 61502,
            FitTexture = true,
            Tooltip = "Failed to load, this module has been disabled",
        };
        erroringImageNode.AttachNode(this);

        modificationNameNode = new TextNode {
            TextFlags = TextFlags.AutoAdjustNodeSize | TextFlags.Ellipsis,
            AlignmentType = AlignmentType.BottomLeft,
            TextColor = ColorHelper.GetColor(1),
        };
        modificationNameNode.AttachNode(this);

        experimentalImageNode = new IconImageNode {
            IconId = 60073,
            FitTexture = true,
            Tooltip = "Caution, this feature is experimental.\nMay contain bugs or crash your game.",
        };
        experimentalImageNode.AttachNode(this);
        
        authorNamesNode = new TextNode {
            FontType = FontType.Axis,
            TextFlags = TextFlags.AutoAdjustNodeSize | TextFlags.Ellipsis,
            AlignmentType = AlignmentType.TopLeft,
            TextColor = ColorHelper.GetColor(3),
        };
        authorNamesNode.AttachNode(this);

        reloadButtonNode = new CircleButtonNode {
            Icon = ButtonIcon.Refresh,
            Tooltip = "Retry compatability check",
            OnClick = () => {
                System.ModificationManager.ReloadConflictedModules();
                reloadButtonNode?.HideTooltip();
            },
        };
        reloadButtonNode.AttachNode(this);
        
        configButtonNode = new CircleButtonNode {
            Icon = ButtonIcon.GearCog,
            Tooltip = "Open configuration window",
            OnClick = () => {
                Modification?.Modification.OpenConfigAction?.Invoke();
                OnClick?.Invoke();
            },
        };
        configButtonNode.AttachNode(this);

        CollisionNode.DrawFlags = DrawFlags.ClickableCursor;
        
        CollisionNode.AddEvent(AtkEventType.MouseOver, () => {
            if (!IsSelected) {
                IsHovered = true;
            }
        });
        
        CollisionNode.AddEvent(AtkEventType.MouseDown, () => {
            OnClick?.Invoke();
        });
        
        CollisionNode.AddEvent(AtkEventType.MouseOut, () => {
            IsHovered = false;
        });
    }

    public ModificationInfo ModificationInfo => Modification.Modification.ModificationInfo;
    
    public required LoadedModification Modification {
        get;
        set {
            field = value;
            modificationNameNode.String = value.Modification.ModificationInfo.DisplayName;
            authorNamesNode.String = $"By {string.Join(", ", value.Modification.ModificationInfo.Authors)}";

            RefreshConfigWindowButton();

            checkboxNode.IsChecked = value.State is LoadedState.Enabled;

            experimentalImageNode.IsVisible = value.Modification.IsExperimental;

            UpdateDisabledState();
        }
    }
    
    private void ToggleModification(bool shouldEnableModification) {
        if (shouldEnableModification && Modification.State is LoadedState.Disabled) {
            ModificationManager.TryEnableModification(Modification);
        }
        else if (!shouldEnableModification && Modification.State is LoadedState.Enabled) {
            ModificationManager.TryDisableModification(Modification);
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
            configButtonNode.IsEnabled = Modification.State is LoadedState.Enabled;
        }
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        hoveredBackgroundNode.Size = Size;
        selectedBackgroundNode.Size = Size;

        checkboxNode.Size = new Vector2(Height, Height) * 3.0f / 4.0f;
        checkboxNode.Position = new Vector2(Height, Height) / 8.0f;

        modificationNameNode.Height = Height / 2.0f;
        modificationNameNode.Position = new Vector2(Height + Height / 3.0f, 0.0f);
        
        experimentalImageNode.Size = new Vector2(16.0f, 16.0f);
        experimentalImageNode.Position = new Vector2(modificationNameNode.X, modificationNameNode.Height);
        
        authorNamesNode.Height = Height / 2.0f;
        authorNamesNode.Position = new Vector2(Height * 2.0f, Height / 2.0f);

        configButtonNode.Size = new Vector2(Height * 2.0f / 3.0f, Height * 2.0f / 3.0f);
        configButtonNode.Position = new Vector2(Width - Height, Height / 2.0f - configButtonNode.Height / 2.0f);
        
        reloadButtonNode.Size = new Vector2(Height * 2.0f / 3.0f, Height * 2.0f / 3.0f);
        reloadButtonNode.Position = new Vector2(Width - Height * 1.75f - 2.0f, Height / 2.0f - configButtonNode.Height / 2.0f);

        erroringImageNode.Size = checkboxNode.Size - new Vector2(4.0f, 4.0f);
        erroringImageNode.Position = checkboxNode.Position + new Vector2(1.0f, 3.0f);
    }

    public void UpdateDisabledState() {
        if (Modification.State is LoadedState.Errored or LoadedState.CompatError) {
            checkboxNode.IsEnabled = false;
            erroringImageNode.IsVisible = true;
            erroringImageNode.Tooltip = Modification.ErrorMessage;

            reloadButtonNode.IsVisible = Modification.State is LoadedState.CompatError;
        }
        else {
            checkboxNode.IsEnabled = true;
            erroringImageNode.IsVisible = false;
            reloadButtonNode.IsVisible = false;
        }

        Addon.UpdateCollisionForNode(this);

        checkboxNode.IsChecked = Modification.State is LoadedState.Enabled;
        configButtonNode.IsEnabled = Modification.State is LoadedState.Enabled;
        configButtonNode.IsVisible = Modification.Modification.OpenConfigAction is not null;

        RefreshConfigWindowButton();
    }
}
