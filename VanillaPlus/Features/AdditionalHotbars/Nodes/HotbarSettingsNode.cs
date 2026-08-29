using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Components.ConfigurationNodes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using Lumina.Text.ReadOnly;
using VanillaPlus.Features.AdditionalHotbars.Config;

namespace VanillaPlus.Features.AdditionalHotbars.Nodes;

public class HotbarSettingsNode : EntryConfigurationNode<HotbarConfig> {

    protected override void PopulateEntryData(HotbarConfig entry) {
        currentEntry = entry;

        nameInputNode.OnInputReceived = null;
        nameInputNode.String = entry.Name;
        nameInputNode.OnInputReceived = OnNameChanged;

        widthInputNode.OnValueUpdate = null;
        widthInputNode.Value = entry.Width;
        widthInputNode.OnValueUpdate = OnWidthChanged;

        heightInputNode.OnValueUpdate = null;
        heightInputNode.Value = entry.Height;
        heightInputNode.OnValueUpdate = OnHeightChanged;

        horizontalSpacingInputNode.OnValueUpdate = null;
        horizontalSpacingInputNode.Value = entry.HorizontalSpacing;
        horizontalSpacingInputNode.OnValueUpdate = OnHorizontalSpacingChanged;

        verticalSpacingInputNode.OnValueUpdate = null;
        verticalSpacingInputNode.Value = entry.VerticalSpacing;
        verticalSpacingInputNode.OnValueUpdate = OnVerticalSpacingChanged;

        enableToggleNode.OnClick = null;
        enableToggleNode.IsChecked = entry.IsEnabled;
        enableToggleNode.OnClick = OnEnableToggled;

        movingToggleNode.OnClick = null;
        movingToggleNode.IsChecked = entry.MovingEnabled;
        movingToggleNode.OnClick = OnMovingToggled;
    }

    private void OnNameChanged(ReadOnlySeString newName) {
        currentEntry?.Name = newName.ToString();
        SaveConfig?.Invoke();
    }

    private void OnWidthChanged(int newWidth) {
        if (currentEntry is null) return;

        currentEntry.Width = newWidth;
        currentEntry.NeedsRebuildLayout = true;

        currentEntry.Slots.Clear();
        currentEntry.Slots = [.. Enumerable.Repeat(new SlotData(), newWidth * currentEntry.Height)];

        SaveConfig?.Invoke();
    }

    private void OnHeightChanged(int newHeight) {
        if (currentEntry is null) return;

        currentEntry.Height = newHeight;
        currentEntry.NeedsRebuildLayout = true;
        currentEntry.Slots.Clear();
        currentEntry.Slots = [.. Enumerable.Repeat(new SlotData(), currentEntry.Width * newHeight)];

        SaveConfig?.Invoke();
    }

    private void OnHorizontalSpacingChanged(int newHorizontalSpacing) {
        currentEntry?.HorizontalSpacing = newHorizontalSpacing;
        currentEntry?.NeedsRecalcLayout = true;
        SaveConfig?.Invoke();
    }

    private void OnVerticalSpacingChanged(int newVerticalSpacing) {
        currentEntry?.VerticalSpacing = newVerticalSpacing;
        currentEntry?.NeedsRecalcLayout = true;
        SaveConfig?.Invoke();
    }

    private void OnMovingToggled(bool isMovingEnabled) {
        currentEntry?.MovingEnabled = isMovingEnabled;
    }

    private void OnEnableToggled(bool isEnabled) {
        currentEntry?.IsEnabled = isEnabled;
        SaveConfig?.Invoke();
    }

    public HotbarSettingsNode() {
        layoutNode = new VerticalListNode {
            FitWidth = true,
            FirstItemSpacing = 10.0f,
            ItemSpacing = 4.0f,
            InitialNodes = [
                new CategoryTextNode {
                    String = "Hotbar Name",
                },
                nameInputNode = new TextInputNode { // Name Configuration
                    Height = 28.0f,
                    PlaceholderString = "Name...",
                },
                new HorizontalFlexNode { // Hotbar Size Configuration
                    Height = 60.0f,
                    AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
                    ItemSpacing = 16.0f,
                    InitialNodes = [
                        new VerticalListNode {
                            FitWidth = true,
                            InitialNodes = [
                                new CategoryTextNode {
                                    Height = 28.0f,
                                    String = "Width",
                                },
                                widthInputNode = new NumericInputNode {
                                    Height = 28.0f,
                                    Min = 1,
                                    Max = 50,
                                },
                            ],
                        },
                        new VerticalListNode {
                            FitWidth = true,
                            InitialNodes = [
                                new CategoryTextNode {
                                    Height = 28.0f,
                                    String = "Height",
                                },
                                heightInputNode = new NumericInputNode {
                                    Height = 28.0f,
                                    Min = 1,
                                    Max = 50,
                                },
                            ],
                        },
                    ],
                },
                new TextNode {
                    Height = 26.0f,
                    FontSize = 10,
                    AlignmentType = AlignmentType.Center,
                    String = "Warning, slot configurations will be lost when changing Width/Height",
                },
                new HorizontalFlexNode { // Slot Spacing Configuration
                    Height = 60.0f,
                    AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
                    ItemSpacing = 16.0f,
                    InitialNodes = [
                        new VerticalListNode {
                            FitWidth = true,
                            InitialNodes = [
                                new CategoryTextNode {
                                    Height = 28.0f,
                                    String = "Horizontal Spacing",
                                },
                                horizontalSpacingInputNode = new NumericInputNode {
                                    Height = 28.0f,
                                    Min = -7,
                                    Max = 128,
                                },
                            ],
                        },
                        new VerticalListNode {
                            FitWidth = true,
                            InitialNodes = [
                                new CategoryTextNode {
                                    Height = 28.0f,
                                    String = "Vertical Spacing",
                                },
                                verticalSpacingInputNode = new NumericInputNode {
                                    Height = 28.0f,
                                    Min = -6,
                                    Max = 128,
                                },
                            ],
                        },
                    ],
                },
                new ResNode{ Height = 8.0f },
                enableToggleNode = new CheckboxNode {
                    Height = 28.0f,
                    String = "Enable Hotbar",
                },
                movingToggleNode = new CheckboxNode {
                    Height = 28.0f,
                    String = "Enable Moving Hotbar",
                },
            ],
        };
        layoutNode.AttachNode(ConfigurationContentNode);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        layoutNode.Size = Size;
        layoutNode.RecalculateLayout();
    }

    private HotbarConfig? currentEntry;
    private readonly VerticalListNode layoutNode;
    private readonly TextInputNode nameInputNode;
    private readonly NumericInputNode widthInputNode;
    private readonly NumericInputNode heightInputNode;
    private readonly NumericInputNode horizontalSpacingInputNode;
    private readonly NumericInputNode verticalSpacingInputNode;
    private readonly CheckboxNode movingToggleNode;
    private readonly CheckboxNode enableToggleNode;
}
