using System;
using System.Numerics;
using KamiToolKit.Classes;
using KamiToolKit.Components.Icons;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.CurrencyWarning.Nodes;

/// <summary>
/// Configuration node for <see cref="CurrencyWarning"/>'s general <see cref="CurrencyWarningConfig"/>.
/// This node is populated in the "General" tab of <see cref="CurrencyWarning.configAddon"/>.
/// </summary>
public class CurrencyWarningGeneralConfigNode : ResNode {

    private readonly HorizontalFlexNode mainLayoutContainer;

    public CurrencyWarningGeneralConfigNode() {
        var config = CurrencyWarning.Config;
        if (config is null) throw new Exception("Somehow tried to load a config node with an invalid config");

        mainLayoutContainer = new HorizontalFlexNode {
            AlignmentFlags = FlexFlags.FitHeight | FlexFlags.FitWidth,
            InitialNodes = [
                new TabbedVerticalListNode {
                    InitialTabbedNodes = [
                        new TabbedListEntry(1, [
                            new CategoryTextNode {
                                Size = new Vector2(200.0f, 26.0f),
                                String = Strings.CurrencyWarning_CategoryGeneral,
                            },
                        ]),
                        new TabbedListEntry(2, [
                            new CheckboxNode {
                                Size = new Vector2(200.0f, 26.0f),
                                String = Strings.CurrencyWarning_EnableMoving,
                                IsChecked = config.IsMoveable,
                                OnClick = newValue => {
                                    config.IsMoveable = newValue;
                                    config.Save();
                                },
                            },
                            new CheckboxNode {
                                Size = new Vector2(200.0f, 26.0f),
                                String = Strings.CurrencyWarning_PlayAnimations,
                                IsChecked = config.PlayAnimations,
                                OnClick = newValue => {
                                    config.PlayAnimations = newValue;
                                    config.Save();
                                },
                            },
                            new CheckboxNode {
                                Size = new Vector2(200.0f, 26.0f),
                                String = Strings.CurrencyWarning_LabelHideInDuties,
                                IsChecked = config.HideInDuties,
                                OnClick = newValue => {
                                    config.HideInDuties = newValue;
                                    config.Save();
                                },
                            },
                        ]),
                        new TabbedListEntry(1, new CategoryTextNode {
                            Size = new Vector2(200.0f, 26.0f),
                            String = Strings.CurrencyWarning_IconScale,
                        }),
                        new TabbedListEntry(2, new FloatSliderNode {
                            Size = new Vector2(200.0f, 26.0f),
                            Min = 0.25f,
                            Max = 4.00f,
                            Value = config.Scale,
                            OnValueChanged = newValue => {
                                config.Scale = newValue;
                                config.Save();
                            },
                        }),
                    ],
                },
                new TabbedVerticalListNode {
                    InitialTabbedNodes = [
                        new TabbedListEntry(1, new CategoryTextNode {
                            Size = new Vector2(200.0f, 26.0f),
                            String = Strings.CurrencyWarning_CategoryBelowTargetWarning,
                        }),
                        new TabbedListEntry(2, [
                            new ColorEditNode {
                                Size = new Vector2(200.0f, 26.0f),
                                String = Strings.CurrencyWarning_LabelTooltipTextColor,
                                CurrentColor = config.LowColor,
                                OnColorConfirmed = newColor => {
                                    config.LowColor = newColor;
                                    config.Save();
                                },
                            },
                            new IconSelectionNode([60073u, 60357u, 230402u]) {
                                Size = new Vector2(200.0f, 100.0f),
                                SelectedIcon = config.LowIcon,
                                OnIconChanged = newIcon => {
                                    config.LowIcon = newIcon;
                                    config.Save();
                                },
                            },
                        ]),
                        new TabbedListEntry(1, new CategoryTextNode {
                            Size = new Vector2(200.0f, 26.0f),
                            String = Strings.CurrencyWarning_CategoryAboveTargetWarning,
                        }),
                        new TabbedListEntry(2, [
                            new ColorEditNode {
                                Size = new Vector2(200.0f, 26.0f),
                                String = Strings.CurrencyWarning_LabelTooltipTextColor,
                                CurrentColor = config.HighColor,
                                OnColorConfirmed = newColor => {
                                    config.HighColor = newColor;
                                    config.Save();
                                },
                            },
                            new IconSelectionNode([60074u, 63908u, 230403u]) {
                                Size = new Vector2(200.0f, 100.0f),
                                SelectedIcon = config.HighIcon,
                                OnIconChanged = newIcon => {
                                    config.HighIcon = newIcon;
                                    config.Save();
                                },
                            },
                        ]),
                    ],
                },
            ],
        };

        mainLayoutContainer.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        mainLayoutContainer.Size = Size;
        mainLayoutContainer.RecalculateLayout();
    }
}
