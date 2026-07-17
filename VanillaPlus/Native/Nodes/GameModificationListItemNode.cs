using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;
using Addon = VanillaPlus.Utilities.Addon;

namespace VanillaPlus.Native.Nodes;

public class GameModificationListItemNode : ListItemNode<LoadedModification>, IListItemNode {

    public static float ItemHeight => 48.0f;

    private readonly ResNode checkboxContainerNode;
    private readonly ResNode labelsContainerNode;
    private readonly ResNode buttonsContainerNode;

    private readonly CheckboxNode checkboxNode;
    private readonly IconImageNode erroringImageNode;

    private readonly TextNode titleTextNode;
    private readonly IconImageNode experimentalImageNode;

    private readonly TextNode authorTextNode;

    private readonly CircleButtonNode openConfigButton;
    private readonly CircleButtonNode refreshCompatabilityButton;

    public GameModificationListItemNode() {
        checkboxContainerNode = new ResNode();
        checkboxContainerNode.AttachNode(this);

        checkboxNode = new CheckboxNode {
            OnClick = ToggleGameModification,
        };

        unsafe {
            checkboxNode.AddEvent(AtkEventType.InputReceived, OnCheckboxInputReceived);
        }

        checkboxNode.AttachNode(checkboxContainerNode);

        erroringImageNode = new IconImageNode {
            IconId = 61502,
            FitTexture = true,
            TextTooltip = Strings.Tooltip_ModificationFailedToLoad,
            IsVisible = false,
        };
        erroringImageNode.AttachNode(checkboxContainerNode);

        labelsContainerNode = new ResNode();
        labelsContainerNode.AttachNode(this);

        titleTextNode = new TextNode {
            TextFlags = TextFlags.Ellipsis,
            AlignmentType = AlignmentType.BottomLeft,
        };
        titleTextNode.AttachNode(labelsContainerNode);

        experimentalImageNode = new IconImageNode {
            IconId = 60073,
            FitTexture = true,
            IsVisible = false,
            TextTooltip = Strings.Tooltip_ExperimentalFeature,
        };
        experimentalImageNode.AttachNode(labelsContainerNode);

        authorTextNode = new TextNode {
            FontType = FontType.Axis,
            TextFlags = TextFlags.Ellipsis,
            TextColor = ColorHelper.GetColor(3),
        };
        authorTextNode.AttachNode(labelsContainerNode);

        buttonsContainerNode = new ResNode();
        buttonsContainerNode.AttachNode(this);

        openConfigButton = new CircleButtonNode {
            Icon = CircleButtonIcon.GearCog,
            IsVisible = false,
            TextTooltip = Strings.Tooltip_OpenConfiguration,
        };
        openConfigButton.AttachNode(buttonsContainerNode);

        refreshCompatabilityButton = new CircleButtonNode {
            Icon = CircleButtonIcon.Refresh,
            IsVisible = false,
            TextTooltip = Strings.Tooltip_RetryCompatibility,
            OnClick = () => {
                refreshCompatabilityButton?.IsEnabled = false;
                Task.Run(async () => {
                    await System.ModificationManager.ReloadConflictedModules();
                    refreshCompatabilityButton?.IsEnabled = true;

                    if (ItemData is not null) {
                        await IFramework.Get().RunSafely(() => {
                            SetNodeData(ItemData);
                            Addon.UpdateCollisionForNode(this);
                        });
                    }
                });
                refreshCompatabilityButton?.HideTooltip();
            },

        };
        refreshCompatabilityButton.AttachNode(buttonsContainerNode);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        checkboxContainerNode.Size = new Vector2(Height, Height);
        checkboxContainerNode.Position = new Vector2(0.0f, 0.0f);

        buttonsContainerNode.Size = new Vector2(Height * 4.0f / 3.0f, Height );
        buttonsContainerNode.Position = new Vector2(Width - buttonsContainerNode.Width, 0.0f);

        labelsContainerNode.Size = new Vector2(Width - checkboxContainerNode.Width - buttonsContainerNode.Width, Height);
        labelsContainerNode.Position = new Vector2(checkboxContainerNode.Bounds.Right, 0.0f);

        checkboxNode.Size = checkboxContainerNode.Size * 2.0f / 3.0f;
        checkboxNode.Position = checkboxContainerNode.Size / 6.0f;

        erroringImageNode.Size = checkboxNode.Size;
        erroringImageNode.Position = checkboxNode.Position;

        titleTextNode.Size = new Vector2(labelsContainerNode.Width, Height / 2.0f);
        titleTextNode.Position = new Vector2(0.0f, 0.0f);

        authorTextNode.Size = new Vector2(labelsContainerNode.Width - Height / 2.0f, Height / 2.0f);
        authorTextNode.Position = new Vector2(0.0f, Height / 2.0f);

        experimentalImageNode.Size = new Vector2(Height / 3.0f, Height / 3.0f);
        experimentalImageNode.Position = new Vector2(authorTextNode.Width + Height / 12.0f, Height / 2.0f + Height / 12.0f);

        refreshCompatabilityButton.Size = new Vector2(Height * 2.0f / 3.0f, Height * 2.0f / 3.0f);
        refreshCompatabilityButton.Position = new Vector2(buttonsContainerNode.Width / 4.0f - refreshCompatabilityButton.Width / 2.0f, Height / 6.0f);

        openConfigButton.Size = new Vector2(Height * 2.0f / 3.0f, Height * 2.0f / 3.0f);
        openConfigButton.Position = new Vector2(buttonsContainerNode.Width * 3.0f / 4.0f - openConfigButton.Width / 2.0f, Height / 6.0f);
    }

    protected override void SetNodeData(LoadedModification itemData) {
        titleTextNode.String = itemData.Modification.ModificationInfo.DisplayName;

        var authorList = string.Join(", ", itemData.Modification.ModificationInfo.Authors);
        authorTextNode.String = Strings.Label_ModAuthorBy.Format(authorList);

        checkboxNode.IsChecked = itemData.State is LoadedState.Enabled;
        checkboxNode.IsEnabled = !itemData.State.IsTrouble;

        erroringImageNode.IsVisible = itemData.State.IsTrouble;

        refreshCompatabilityButton.IsVisible = itemData.State is LoadedState.CompatError;

        var collisionUpdateNeeded = false;

        if (erroringImageNode.TextTooltip != itemData.ErrorMessage) {
            erroringImageNode.TextTooltip = itemData.ErrorMessage;
            collisionUpdateNeeded = true;
        }

        if (experimentalImageNode.IsVisible != itemData.Modification.IsExperimental) {
            experimentalImageNode.IsVisible = itemData.Modification.IsExperimental;
            collisionUpdateNeeded = true;
        }

        var wantsConfigButton = itemData.Modification is { OpenConfigAction: not null } or { OpenConfigAsync: not null };
        var hasConfigButton = openConfigButton.IsVisible;

        if (wantsConfigButton && !hasConfigButton) {
            collisionUpdateNeeded = true;
        }

        switch (itemData.State) {
            case LoadedState.Enabled when itemData.Modification.OpenConfigAction is { } openConfig:
                openConfigButton.OnClick = () => openConfig();
                openConfigButton.IsVisible = true;
                break;

            case LoadedState.Enabled when itemData.Modification.OpenConfigAsync is { } openConfigAsync:
                openConfigButton.OnClick = () => {
                    try {
                        Task.Run((Func<Task?>) openConfigAsync);
                    }
                    catch (Exception e) {
                        IPluginLog.Get().Exception(e);
                    }
                };
                openConfigButton.IsVisible = true;
                break;

            case LoadedState.Unknown:
            case LoadedState.Disabled:
            case LoadedState.Errored:
            case LoadedState.CompatError:
            case LoadedState.ForceDisabled:
            default:
                openConfigButton.OnClick = null;
                openConfigButton.IsVisible = false;
                break;
        }

        if (collisionUpdateNeeded) {
            HideTooltip();
            Addon.UpdateCollisionForNode(this);
        }
    }

    public override void ProcessNav(int index, int up, int down, int left, int right) {
        base.ProcessNav(index, up, down, left, right);

        checkboxNode.NavIndex = index;
        refreshCompatabilityButton.NavIndex = index + 1;
        openConfigButton.NavIndex = index + 2;

        checkboxNode.NavUp = up;
        checkboxNode.NavDown = down;
        checkboxNode.NavRight = refreshCompatabilityButton.NavIndex;
        checkboxNode.NavLeft = left;

        refreshCompatabilityButton.NavUp = up;
        refreshCompatabilityButton.NavDown = down;
        refreshCompatabilityButton.NavRight = openConfigButton.NavIndex;
        refreshCompatabilityButton.NavLeft = checkboxNode.NavIndex;

        openConfigButton.NavUp = up;
        openConfigButton.NavDown = down;
        openConfigButton.NavRight = right;
        openConfigButton.NavLeft = refreshCompatabilityButton.NavIndex;
    }

    private unsafe void OnCheckboxInputReceived(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        if (eventType is not AtkEventType.InputReceived) return;

        if (atkEventData->InputData.State is InputState.Down) {
            IsHovered = false;
        }
        else if (atkEventData->InputData.State is InputState.Up or InputState.Held) {
            IsHovered = true;
            OnClick?.Invoke(this);
        }
    }

    private void ToggleGameModification(bool isEnabled) {
        if (ItemData is null) return;

        Task.Run(async () => {
            await ModificationManager.TryToggleModification(ItemData);
            await IFramework.Get().RunSafely(() => SetNodeData(ItemData));
        });
    }
}
