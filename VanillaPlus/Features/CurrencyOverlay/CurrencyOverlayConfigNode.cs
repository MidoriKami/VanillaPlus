using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addons.Parts;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;

namespace VanillaPlus.Features.CurrencyOverlay;

public class CurrencyOverlayConfigNode : ConfigNode<CurrencySetting> {

    private TextNode itemNameTextNode;
    private IconImageNode iconImageNode;
    
    private CheckboxNode enableLowLimitCheckbox;
    private NumericInputNode lowLimitInputNode;
    
    private CheckboxNode enableHighLimitCheckbox;
    private NumericInputNode highLimitInputNode;
    
    private CheckboxNode reverseIconCheckbox;

    private CheckboxNode allowMovingCheckbox;

    private TextNode scaleTextNode;
    private SliderNode scaleSliderNode;
    
    
    public CurrencyOverlayConfigNode() {
        iconImageNode = new IconImageNode {
            FitTexture = true,
            Alpha = 0.1f,
            IsVisible = true,
        };
        System.NativeController.AttachNode(iconImageNode, this);

        itemNameTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 18,
            IsVisible = true,
        };
        System.NativeController.AttachNode(itemNameTextNode, this);

        enableLowLimitCheckbox = new CheckboxNode {
            String = "Warn when below limit",
            IsVisible = true,
            OnClick = enabled => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.EnableLowLimit = enabled;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(enableLowLimitCheckbox, this);

        lowLimitInputNode = new NumericInputNode {
            IsVisible = true,
            OnValueUpdate = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.LowLimit = newValue;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(lowLimitInputNode, this);
        
        enableHighLimitCheckbox = new CheckboxNode {
            IsVisible = true,
            String = "Warn when above limit",
            OnClick = enabled => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.EnableHighLimit = enabled;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(enableHighLimitCheckbox, this);
        
        highLimitInputNode = new NumericInputNode {
            IsVisible = true,
            OnValueUpdate = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.HighLimit = newValue;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(highLimitInputNode, this);
        
        reverseIconCheckbox = new CheckboxNode {
            IsVisible = true,
            String = "Reverse icon position",
            OnClick = enabled => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.IconReversed = enabled;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(reverseIconCheckbox, this);
        
        allowMovingCheckbox = new CheckboxNode {
            IsVisible = true,
            String = "Enable moving overlay element",
            OnClick = enabled => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.IsNodeMoveable = enabled;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(allowMovingCheckbox, this);

        scaleTextNode = new LabelTextNode {
            String = "Scale",
            IsVisible = true,
        };
        System.NativeController.AttachNode(scaleTextNode, this);
        
        scaleSliderNode = new SliderNode {
            Range = 50..300,
            DecimalPlaces = 2,
            IsVisible = true,
            OnValueChanged = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.Scale = newValue / 100.0f;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(scaleSliderNode, this);
    }

    protected override void OptionChanged(CurrencySetting? option) {
        if (option is null) return;

        itemNameTextNode.String = option.GetLabel();
        iconImageNode.IconId = option.GetIconId() ?? 0;
        iconImageNode.IsVisible = iconImageNode.IconId is not 0;
        enableLowLimitCheckbox.IsChecked = option.EnableLowLimit;
        lowLimitInputNode.Value = option.LowLimit;
        enableHighLimitCheckbox.IsChecked = option.EnableHighLimit;
        highLimitInputNode.Value = option.HighLimit;
        reverseIconCheckbox.IsChecked = option.IconReversed;
        allowMovingCheckbox.IsChecked = option.IsNodeMoveable;
        scaleSliderNode.Value = (int)(option.Scale * 100.0f);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        itemNameTextNode.Size = new Vector2(Width, 24.0f);
        itemNameTextNode.Position = new Vector2(0.0f, 50.0f);

        iconImageNode.Size = new Vector2(Width - 40.0f, Height - 40.0f);
        iconImageNode.Position = new Vector2(20.0f, 20.0f);

        enableLowLimitCheckbox.Size = new Vector2(Width, 24.0f);
        enableLowLimitCheckbox.Position = new Vector2(20.0f, 125.0f);

        lowLimitInputNode.Size = new Vector2(150.0f, 24.0f);
        lowLimitInputNode.Position = new Vector2(65.0f, 150.0f);
        
        enableHighLimitCheckbox.Size = new Vector2(Width, 24.0f);
        enableHighLimitCheckbox.Position = new Vector2(20.0f, 200.0f);
        
        highLimitInputNode.Size = new Vector2(150.0f, 24.0f);
        highLimitInputNode.Position = new Vector2(65.0f, 225.0f);

        reverseIconCheckbox.Size = new Vector2(Width, 24.0f);
        reverseIconCheckbox.Position = new Vector2(20.0f, 275.0f);
        
        allowMovingCheckbox.Size = new Vector2(Width, 24.0f);
        allowMovingCheckbox.Position = new Vector2(20.0f, 325.0f);

        scaleTextNode.Size = new Vector2(100.0f, 24.0f);
        scaleTextNode.Position = new Vector2(20.0f, 375.0f);
        
        scaleSliderNode.Size = new Vector2(250.0f, 24.0f);
        scaleSliderNode.Position = new Vector2(20.0f, 405.0f);
    }
}
