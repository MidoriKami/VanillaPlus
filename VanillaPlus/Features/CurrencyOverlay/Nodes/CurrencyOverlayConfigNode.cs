using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;

namespace VanillaPlus.Features.CurrencyOverlay.Nodes;

public class CurrencyOverlayConfigNode : ConfigNode<CurrencySetting> {
    private readonly TextNode itemNameTextNode;
    private readonly IconImageNode iconImageNode;
    private readonly CheckboxNode enableLowLimitCheckbox;
    private readonly NumericInputNode lowLimitInputNode;
    private readonly CheckboxNode enableHighLimitCheckbox;
    private readonly NumericInputNode highLimitInputNode;
    private readonly CheckboxNode reverseIconCheckbox;
    private readonly CheckboxNode reverseTextCheckbox;
    private readonly CheckboxNode allowMovingCheckbox;
    private readonly SliderNode scaleSliderNode;
    private readonly CheckboxNode fadeIfNoWarningsCheckbox;
    private readonly SliderNode fadeSliderNode;
    private readonly TabbedVerticalListNode layoutNode;
    
    public CurrencyOverlayConfigNode() {
        iconImageNode = new IconImageNode {
            FitTexture = true,
            Alpha = 0.1f,
        };
        iconImageNode.AttachNode(this);

        itemNameTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 18,
        };
        itemNameTextNode.AttachNode(this);

        layoutNode = new TabbedVerticalListNode {
            ItemVerticalSpacing = 6.0f,
            FitWidth = true,
        };
        
        layoutNode.AddNode(0, enableLowLimitCheckbox = new CheckboxNode {
            Height = 24.0f,
            String = "Warn when below limit",
            OnClick = enabled => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.EnableLowLimit = enabled;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        });
        
        layoutNode.AddNode(1, lowLimitInputNode = new NumericInputNode {
            Height = 24.0f,
            OnValueUpdate = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.LowLimit = newValue;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        });
        
        layoutNode.AddNode(0, enableHighLimitCheckbox = new CheckboxNode {
            Height = 24.0f,
            String = "Warn when above limit",
            OnClick = enabled => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.EnableHighLimit = enabled;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        });
        
        layoutNode.AddNode(1, highLimitInputNode = new NumericInputNode {
            Height = 24.0f,
            OnValueUpdate = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.HighLimit = newValue;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        });
        
        layoutNode.AddNode(0, [
            reverseIconCheckbox = new CheckboxNode {
                Height = 24.0f,
                String = "Reverse icon position",
                OnClick = enabled => {
                    if (ConfigurationOption is not null) {
                        ConfigurationOption.IconReversed = enabled;
                        OnConfigChanged?.Invoke(ConfigurationOption);
                    }
                },
            },
            reverseTextCheckbox = new CheckboxNode {
                Height = 24.0f,
                String = "Reverse text position",
                OnClick = enabled => {
                    if (ConfigurationOption is not null) {
                        ConfigurationOption.TextReversed = enabled;
                        OnConfigChanged?.Invoke(ConfigurationOption);
                    }
                },
            },
            allowMovingCheckbox = new CheckboxNode {
                Height = 24.0f,
                String = "Enable moving overlay element",
                OnClick = enabled => {
                    if (ConfigurationOption is not null) {
                        ConfigurationOption.IsNodeMoveable = enabled;
                        OnConfigChanged?.Invoke(ConfigurationOption);
                    }
                },
            },
            new CategoryTextNode {
                Height = 24.0f,
                String = "Scale",
            },
            scaleSliderNode = new SliderNode {
                Height = 24.0f,
                Range = 50..300,
                DecimalPlaces = 2,
                OnValueChanged = newValue => {
                    if (ConfigurationOption is not null) {
                        ConfigurationOption.Scale = newValue / 100.0f;
                        OnConfigChanged?.Invoke(ConfigurationOption);
                    }
                },
            },
            fadeIfNoWarningsCheckbox = new CheckboxNode {
                Height = 24.0f,
                String = "Fade if no warnings",
                OnClick = enabled => {
                    if (ConfigurationOption is not null) {
                        ConfigurationOption.FadeIfNoWarnings = enabled;
                        OnConfigChanged?.Invoke(ConfigurationOption);
                    }
                },
            },
            new CategoryTextNode {
                Height = 24.0f,
                String = Strings.CurrencyOverlay_LabelFadePercentage,
            },
            fadeSliderNode = new SliderNode {
                Height = 24.0f,
                Range = ..90,
                DecimalPlaces = 2,
                OnValueChanged = newValue => {
                    if (ConfigurationOption is not null) {
                        ConfigurationOption.FadePercent = newValue / 100.0f;
                        OnConfigChanged?.Invoke(ConfigurationOption);
                    }
                },
            },
        ]);
        
        layoutNode.AttachNode(this);
    }

    protected override void OptionChanged(CurrencySetting? option) {
        if (option is null) return;

        var itemInfo = Services.DataManager.GetItem(option.ItemId);
        
        itemNameTextNode.String = itemInfo.Name.ToString();
        
        iconImageNode.IconId = itemInfo.Icon;
        iconImageNode.IsVisible = iconImageNode.IconId is not 0;
        
        lowLimitInputNode.Value = option.LowLimit;
        highLimitInputNode.Value = option.HighLimit;
        
        enableLowLimitCheckbox.IsChecked = option.EnableLowLimit;
        enableHighLimitCheckbox.IsChecked = option.EnableHighLimit;
        reverseIconCheckbox.IsChecked = option.IconReversed;
        reverseTextCheckbox.IsChecked = option.TextReversed;
        allowMovingCheckbox.IsChecked = option.IsNodeMoveable;
        fadeIfNoWarningsCheckbox.IsChecked = option.FadeIfNoWarnings;
        
        scaleSliderNode.Value = (int)(option.Scale * 100.0f);
        fadeSliderNode.Value = (int)(option.FadePercent * 100.0f);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        itemNameTextNode.Size = new Vector2(Width, 24.0f);
        itemNameTextNode.Position = new Vector2(0.0f, 20.0f);
        
        var smallerDimension = Math.Min(Width, Height);
        
        iconImageNode.Size = new Vector2(smallerDimension - 40.0f, smallerDimension - 40.0f);
        iconImageNode.Position = new Vector2(20.0f, Height / 2.0f - iconImageNode.Height / 2.0f);

        layoutNode.Size = new Vector2(Width, Height - itemNameTextNode.Bounds.Bottom - 12.0f);
        layoutNode.Position = new Vector2(0.0f, itemNameTextNode.Bounds.Bottom + 12.0f);
        layoutNode.RecalculateLayout();
    }
}
