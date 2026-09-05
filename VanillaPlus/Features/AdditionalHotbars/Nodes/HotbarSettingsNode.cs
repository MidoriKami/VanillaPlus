using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Components.ConfigurationNodes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using VanillaPlus.Features.AdditionalHotbars.Config;
using VanillaPlus.Native.Addons;
using Keybind = VanillaPlus.Classes.Keybind;

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

        classJobDropDownNode.OnOptionSelected = null;
        classJobDropDownNode.SelectedOption = IDataManager.Get().GetExcelSheet<ClassJob>().GetRow(entry.LinkedClassJob);
        classJobDropDownNode.OnOptionSelected = OnLinkedClassJobChanged;

        scaleNode.OnValueChanged = null;
        scaleNode.Value = entry.Scale;
        scaleNode.OnValueChanged = OnScaleChanged;

        enableToggleNode.OnClick = null;
        enableToggleNode.IsChecked = entry.IsEnabled;
        enableToggleNode.OnClick = OnEnableToggled;

        movingToggleNode.OnClick = null;
        movingToggleNode.IsChecked = entry.MovingEnabled;
        movingToggleNode.OnClick = OnMovingToggled;

        RebuildHotkeyList();
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
        currentEntry.Slots = List<SlotData>.CreateInitialized(newWidth * currentEntry.Height);

        RebuildHotkeyList();

        SaveConfig?.Invoke();
    }

    private void OnHeightChanged(int newHeight) {
        if (currentEntry is null) return;

        currentEntry.Height = newHeight;
        currentEntry.NeedsRebuildLayout = true;
        currentEntry.Slots.Clear();
        currentEntry.Slots = List<SlotData>.CreateInitialized(newHeight * currentEntry.Width);

        RebuildHotkeyList();

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

    private void OnLinkedClassJobChanged(ClassJob obj) {
        currentEntry?.LinkedClassJob = obj.RowId;
        SaveConfig?.Invoke();
    }

    private void OnScaleChanged(float newScale) {
        currentEntry?.Scale = newScale;
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
        tabBarNode = new TabBarNode {
            InitialEntries = [
                new TabBarEntry {
                    Label = "Hotbar Settings",
                    OnClick = () => {
                        hotbarSettingsLayoutNode?.IsVisible = true;
                        hotkeyListNode?.IsVisible = false;
                    },
                },
                new TabBarEntry {
                    Label = "Hotkeys",
                    OnClick = () => {
                        hotbarSettingsLayoutNode?.IsVisible = false;
                        hotkeyListNode?.IsVisible = true;
                    },
                },
            ],
        };
        tabBarNode.AttachNode(ConfigurationContentNode);

        hotbarSettingsLayoutNode = new VerticalListNode {
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
                new CategoryTextNode {
                    Height = 28.0f,
                    String = "Only Show When Active ClassJob is:",
                },
                classJobDropDownNode = new DropDownNode<ClassJob> {
                    Height = 28.0f,
                    Options = [
                        ..IDataManager.Get()
                            .GetExcelSheet<ClassJob>()
                            .Where(job => job.ClassJobCategory.RowId is not 0)
                            .OrderBy(job => job.UIPriority),
                    ],
                    GetLabelFunction = GetClassJobLabel,
                    MaxListOptions = 15,
                },
                new ResNode{ Height = 8.0f },
                new HorizontalListNode {
                    Height = 28.0f,
                    ItemSpacing = 8.0f,
                    FitHeight = true,
                    InitialNodes = [
                        scaleNode = new FloatSliderNode {
                            Width = 300.0f,
                            Min = 0.5f,
                            Max = 2.5f,
                        },
                        new TextNode {
                            String = "Scale",
                            AlignmentType = AlignmentType.TopLeft,
                            FontSize = 14,
                            Width = 100.0f,
                        },
                    ],
                },
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
        hotbarSettingsLayoutNode.AttachNode(ConfigurationContentNode);

        hotkeyListNode = new ScrollingNode<VerticalListNode> {
            ContentNode = {
                FitWidth = true,
                FitContents = true,
                FirstItemSpacing = 10.0f,
                ItemSpacing = 2.0f,
            },
            AutoHideScrollBar = true,
            IsVisible = false,
        };
        hotkeyListNode.AttachNode(ConfigurationContentNode);
    }

    private static ReadOnlySeString GetClassJobLabel(ClassJob entry) {
        if (entry.RowId is 0) return "Any ClassJob";

        return ISeStringEvaluator.Get().EvaluateFromAddon(698, [entry.RowId]);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        tabBarNode.Size = new Vector2(Width, 28.0f);
        tabBarNode.Position = new Vector2(0.0f, 0.0f);

        hotbarSettingsLayoutNode.Size = new Vector2(Width, Height - tabBarNode.Bounds.Bottom - 5.0f);
        hotbarSettingsLayoutNode.Position = new Vector2(0.0f, tabBarNode.Bounds.Bottom + 5.0f);
        hotbarSettingsLayoutNode.RecalculateLayout();

        hotkeyListNode.Size = new Vector2(Width, Height - tabBarNode.Bounds.Bottom - 5.0f);
        hotkeyListNode.Position = new Vector2(0.0f, tabBarNode.Bounds.Bottom + 5.0f);
        hotkeyListNode.RecalculateSizes();
    }

    protected override void Dispose(bool isNativeDestructor) {
        base.Dispose(isNativeDestructor);

        keybindConfigAddon?.Dispose();
    }

    private void RebuildHotkeyList() {
        if (currentEntry is null) return;

        hotkeyListNode.ContentNode.Clear();

        foreach (var (index, slotData) in currentEntry.Slots.Index()) {

            hotkeyListNode.ContentNode.AddNode(new HorizontalFlexNode {
                Height = 28.0f,
                AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
                InitialNodes = [
                    new TextNode {
                        String = $"Row {index / currentEntry.Width + 1, 2} Column {index % currentEntry.Width + 1, 2}",
                    },
                    new TextButtonNode {
                        LabelNode = {
                            FontType = FontType.MiedingerMed,
                        },

                        String = slotData.Hotkey is null ? string.Empty : HotbarNode.GetKeybindText(slotData.Hotkey),
                        OnClick = () => OnChangeKeybindClicked(slotData),
                    },
                ],
            });
        }

        hotkeyListNode.RecalculateSizes();
    }

    private void OnChangeKeybindClicked(SlotData slotData) {
        if (keybindConfigAddon is null) return;

        VirtualKey? modifier = slotData.Hotkey?.KeyModifier switch {
            null => null,
            _ when slotData.Hotkey.Value.KeyModifier.HasFlag(KeyModifierFlag.Ctrl) => VirtualKey.CONTROL,
            _ when slotData.Hotkey.Value.KeyModifier.HasFlag(KeyModifierFlag.Alt) => VirtualKey.MENU,
            _ when slotData.Hotkey.Value.KeyModifier.HasFlag(KeyModifierFlag.Shift) => VirtualKey.SHIFT,
            _ => null,
        };

        keybindConfigAddon.InitialKeybind = new Keybind {
            Key = (VirtualKey?)slotData.Hotkey?.Key ?? VirtualKey.NO_KEY,
            Modifiers = modifier is null ? [] : [ modifier.Value ],
        };

        keybindConfigAddon.OnKeybindChanged = newKeybind => OnKeybindChanged(newKeybind, slotData);

        keybindConfigAddon.Open();
    }

    private void OnKeybindChanged(Keybind newKeybind, SlotData slotData) {
        var rebindKeyModifier = KeyModifierFlag.None;

        // Only allow one modifier key, with the following priority
        if (newKeybind.Modifiers.Contains(VirtualKey.CONTROL)) {
            rebindKeyModifier = KeyModifierFlag.Ctrl;
        }
        else if (newKeybind.Modifiers.Contains(VirtualKey.MENU)) {
            rebindKeyModifier = KeyModifierFlag.Alt;
        }
        else if (newKeybind.Modifiers.Contains(VirtualKey.SHIFT)) {
            rebindKeyModifier = KeyModifierFlag.Shift;
        }

        slotData.Hotkey = new KeySetting {
            Key = (SeVirtualKey)newKeybind.Key,
            KeyModifier = rebindKeyModifier,
        };

        if (newKeybind.Key is VirtualKey.NO_KEY) {
            slotData.Hotkey = null;
        }

        SaveConfig?.Invoke();

        currentEntry?.NeedsRecalcLayout = true;
        RebuildHotkeyList();
    }

    private HotbarConfig? currentEntry;

    private readonly TabBarNode tabBarNode;

    private readonly VerticalListNode hotbarSettingsLayoutNode;
    private readonly TextInputNode nameInputNode;
    private readonly NumericInputNode widthInputNode;
    private readonly NumericInputNode heightInputNode;
    private readonly NumericInputNode horizontalSpacingInputNode;
    private readonly NumericInputNode verticalSpacingInputNode;
    private readonly DropDownNode<ClassJob> classJobDropDownNode;
    private readonly FloatSliderNode scaleNode;
    private readonly CheckboxNode movingToggleNode;
    private readonly CheckboxNode enableToggleNode;

    private readonly ScrollingNode<VerticalListNode> hotkeyListNode;

    private readonly KeybindConfigAddon? keybindConfigAddon = new() {
        InternalName = "KeybindConfig",
        Title = "Hotbar Slot Hotkey",
        InitialKeybind = new Keybind(),
        OnKeybindChanged = _ => { },
    };
}
