using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addons;
using KamiToolKit.Addons.Parts;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using Lumina.Excel.Sheets;

namespace VanillaPlus.Features.CurrencyOverlay;

public class CurrencyOverlayConfigNode : ConfigNode<CurrencySetting> {

    private TextNode noOptionSelectedTextNode;
    
    private TextNode itemNameTextNode;
    private IconImageNode iconImageNode;
    private TextButtonNode changeCurrencyButtonNode;
    
    private CheckboxNode enableLowLimitCheckbox;
    private NumericInputNode lowLimitInputNode;
    
    private CheckboxNode enableHighLimitCheckbox;
    private NumericInputNode highLimitInputNode;
    
    private CheckboxNode reverseIconCheckbox;

    private CheckboxNode allowMovingCheckbox;

    private TextNode scaleTextNode;
    private SliderNode scaleSliderNode;
    
    private LuminaSearchAddon<Item> itemSearchAddon;
    
    public CurrencyOverlayConfigNode() {
        itemSearchAddon = new LuminaSearchAddon<Item> {
            NativeController = System.NativeController,
            InternalName = "LuminaItemSearch",
            Title = "Item Search",
            Size = new Vector2(350.0f, 500.0f),

            GetLabelFunc = item => item.Name.ToString(),
            GetSubLabelFunc = item => item.ItemSearchCategory.Value.Name.ToString(),
            GetIconIdFunc = item => item.Icon,

            SortingOptions = [ "Alphabetical", "Id" ],
            SearchOptions = Services.DataManager.GetCurrencyItems().ToList(),
            SelectionResult = option => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.ItemId = option.RowId;
                    OptionChanged(ConfigurationOption);
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
                itemSearchAddon?.Close();
            },
        };
        
        noOptionSelectedTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 16,
            String = "Select an option on the left",
        };
        System.NativeController.AttachNode(noOptionSelectedTextNode, this);
        
        iconImageNode = new IconImageNode {
            FitTexture = true,
            Alpha = 0.1f,
        };
        System.NativeController.AttachNode(iconImageNode, this);

        itemNameTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 18,
        };
        System.NativeController.AttachNode(itemNameTextNode, this);

        changeCurrencyButtonNode = new TextButtonNode {
            String = "Change Currency",
            OnClick = () => {
                itemSearchAddon.Toggle();
            },
        };
        System.NativeController.AttachNode(changeCurrencyButtonNode, this);

        enableLowLimitCheckbox = new CheckboxNode {
            String = "Warn when below limit",
            OnClick = enabled => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.EnableLowLimit = enabled;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(enableLowLimitCheckbox, this);

        lowLimitInputNode = new NumericInputNode {
            OnValueUpdate = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.LowLimit = newValue;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(lowLimitInputNode, this);
        
        enableHighLimitCheckbox = new CheckboxNode {
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
            OnValueUpdate = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.HighLimit = newValue;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(highLimitInputNode, this);
        
        reverseIconCheckbox = new CheckboxNode {
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
            IsVisible = false,
        };
        System.NativeController.AttachNode(scaleTextNode, this);
        
        scaleSliderNode = new SliderNode {
            Range = 50..300,
            DecimalPlaces = 2,
            OnValueChanged = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.Scale = newValue / 100.0f;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        System.NativeController.AttachNode(scaleSliderNode, this);
    }

    protected override void Dispose(bool disposing, bool isNativeDestructor) {
        if (disposing) {
            itemSearchAddon.Dispose();
            
            base.Dispose(disposing, isNativeDestructor);
        }
    }

    protected override void OptionChanged(CurrencySetting? option) {
        if (option is null) {
            noOptionSelectedTextNode.IsVisible = true;
            itemNameTextNode.IsVisible = false;
            iconImageNode.IsVisible = false;
            enableLowLimitCheckbox.IsVisible = false;
            lowLimitInputNode.IsVisible = false;
            enableHighLimitCheckbox.IsVisible = false;
            highLimitInputNode.IsVisible = false;
            reverseIconCheckbox.IsVisible = false;
            allowMovingCheckbox.IsVisible = false;
            scaleTextNode.IsVisible = false;
            scaleSliderNode.IsVisible = false;
            changeCurrencyButtonNode.IsVisible = false;
            return;
        }

        noOptionSelectedTextNode.IsVisible = false;

        itemNameTextNode.String = option.GetLabel();
        itemNameTextNode.IsVisible = true;
        
        changeCurrencyButtonNode.IsVisible = true;

        iconImageNode.IconId = option.GetIconId() ?? 0;
        iconImageNode.IsVisible = iconImageNode.IconId is not 0;
        
        enableLowLimitCheckbox.IsChecked = option.EnableLowLimit;
        enableLowLimitCheckbox.IsVisible = true;
        
        lowLimitInputNode.Value = option.LowLimit;
        lowLimitInputNode.IsVisible = true;
        
        enableHighLimitCheckbox.IsChecked = option.EnableHighLimit;
        enableHighLimitCheckbox.IsVisible = true;

        highLimitInputNode.Value = option.HighLimit;
        highLimitInputNode.IsVisible = true;
        
        reverseIconCheckbox.IsChecked = option.IconReversed;
        reverseIconCheckbox.IsVisible = true;
        
        allowMovingCheckbox.IsChecked = option.IsNodeMoveable;
        allowMovingCheckbox.IsVisible = true;
        
        scaleTextNode.IsVisible = true;

        scaleSliderNode.Value = (int)(option.Scale * 100.0f);
        scaleSliderNode.IsVisible = true;
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        noOptionSelectedTextNode.Size = Size;
        noOptionSelectedTextNode.Position = Vector2.Zero;

        itemNameTextNode.Size = new Vector2(Width, 24.0f);
        itemNameTextNode.Position = new Vector2(0.0f, 50.0f);
        
        changeCurrencyButtonNode.Size = new Vector2(200.0f, 24.0f);
        changeCurrencyButtonNode.Position = new Vector2(Width / 2.0f - changeCurrencyButtonNode.Size.X / 2.0f, 80.0f);

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
