using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using Addon = VanillaPlus.Utilities.Addon;

namespace VanillaPlus.InternalSystem;

public class GameModificationOptionNode : SelectableNode {

    private readonly CheckboxNode checkboxNode;
    private readonly IconImageNode erroringImageNode;
    private readonly TextNode modificationNameNode;
    private readonly IconImageNode experimentalImageNode;
    private readonly TextNode authorNamesNode;
    private readonly CircleButtonNode reloadButtonNode;
    private readonly CircleButtonNode configButtonNode;

    public GameModificationOptionNode() {
        checkboxNode = new CheckboxNode {
            OnClick = ToggleModification,
        };
        checkboxNode.AttachNode(this);

        erroringImageNode = new IconImageNode {
            IconId = 61502,
            FitTexture = true,
            TextTooltip = Strings.Tooltip_ModificationFailedToLoad,
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
            TextTooltip = Strings.Tooltip_ExperimentalFeature,
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
            TextTooltip = Strings.Tooltip_RetryCompatibility,
            OnClick = () => {
                PluginSystem.ModificationManager.ReloadConflictedModules();
                reloadButtonNode?.HideTooltip();
            },
        };
        reloadButtonNode.AttachNode(this);
        
        configButtonNode = new CircleButtonNode {
            Icon = ButtonIcon.GearCog,
            TextTooltip = Strings.Tooltip_OpenConfiguration,
            OnClick = () => {
                Modification?.Modification.OpenConfigAction?.Invoke();
                OnClick?.Invoke(this);
            },
        };
        configButtonNode.AttachNode(this);
    }

    public ModificationInfo ModificationInfo => Modification.Modification.ModificationInfo;
    
    public required LoadedModification Modification {
        get;
        set {
            field = value;
            modificationNameNode.String = value.Modification.ModificationInfo.DisplayName;
            var authorList = string.Join(", ", value.Modification.ModificationInfo.Authors);
            authorNamesNode.String = Strings.Label_ModAuthorBy.Format(authorList);

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
        
        OnClick?.Invoke(this);
        RefreshConfigWindowButton();
    }

    private void RefreshConfigWindowButton() {
        if (Modification.Modification.OpenConfigAction is not null) {
            configButtonNode.IsVisible = true;
            configButtonNode.IsEnabled = Modification.State is LoadedState.Enabled;
        }
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

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
            erroringImageNode.TextTooltip = Modification.ErrorMessage;

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
