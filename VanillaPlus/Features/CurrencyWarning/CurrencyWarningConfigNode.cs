using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;

namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarningConfigNode : ConfigNode<CurrencyWarningSetting> {
    private readonly TextNode itemNameTextNode;
    private readonly IconImageNode iconImageNode;

    private readonly CheckboxNode lowLimitCheckbox;
    private readonly NumericInputNode lowLimitInput;

    private readonly CheckboxNode highLimitCheckbox;
    private readonly NumericInputNode highLimitInput;

    private readonly TabbedVerticalListNode optionsContainer;

    public CurrencyWarningConfigNode() {
        iconImageNode = new IconImageNode {
            FitTexture = true,
            Alpha = 0.1f,
        };
        iconImageNode.AttachNode(this);

        itemNameTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 20,
        };
        itemNameTextNode.AttachNode(this);

        optionsContainer = new TabbedVerticalListNode {
            ItemVerticalSpacing = 10.0f,
            TabSize = 25.0f,
        };
        optionsContainer.AttachNode(this);

        lowLimitCheckbox = new CheckboxNode {
            String = "Warn when blow this amount:",
            Size = new Vector2(250.0f, 24.0f),
            OnClick = enabled => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.EnableLowLimit = enabled;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        optionsContainer.AddNode(lowLimitCheckbox);

        lowLimitInput = new NumericInputNode {
            Size = new Vector2(160.0f, 24.0f),
            OnValueUpdate = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.LowLimit = newValue;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        optionsContainer.AddNode(1, lowLimitInput);

        optionsContainer.AddNode(new ResNode {
            Height = 10.0f,
        });

        highLimitCheckbox = new CheckboxNode {
            String = "Warn when above this amount:",
            Size = new Vector2(250.0f, 24.0f),
            OnClick = enabled => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.EnableHighLimit = enabled;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        optionsContainer.AddNode(highLimitCheckbox);

        highLimitInput = new NumericInputNode {
            Size = new Vector2(160.0f, 24.0f),
            OnValueUpdate = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.HighLimit = newValue;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        optionsContainer.AddNode(1, highLimitInput);
    }

    protected override void OptionChanged(CurrencyWarningSetting? option) {
        if (option is null) return;

        var item = Services.DataManager.GetItem(option.ItemId);
        itemNameTextNode.String = option.GetLabel();
        iconImageNode.IconId = item.Icon;

        lowLimitCheckbox.IsChecked = option.EnableLowLimit;
        lowLimitInput.Value = option.LowLimit;
        lowLimitInput.Max = (int)item.StackSize;

        highLimitCheckbox.IsChecked = option.EnableHighLimit;
        highLimitInput.Value = option.HighLimit;
        highLimitInput.Max = (int)item.StackSize;

        optionsContainer.RecalculateLayout();
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        itemNameTextNode.Size = new Vector2(Width, 24.0f);
        itemNameTextNode.Position = new Vector2(0.0f, 20.0f);

        iconImageNode.Size = new Vector2(Width - 40.0f, Height - 40.0f);
        iconImageNode.Position = new Vector2(20.0f, 20.0f);

        optionsContainer.Width = Width - 60.0f;
        optionsContainer.Position = new Vector2(40.0f, 80.0f);
        optionsContainer.RecalculateLayout();
    }
}
