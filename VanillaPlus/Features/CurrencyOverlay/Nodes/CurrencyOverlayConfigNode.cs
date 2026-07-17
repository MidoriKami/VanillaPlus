using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Components.Configuration;
using KamiToolKit.Components.ConfigurationNodes;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.CurrencyOverlay.Nodes;

/// <summary>
/// Implementation of <see cref="EntryConfigurationNode{T}"/> for use in <see cref="ConfigurationAddon{T,TU,TV}"/>
/// Used in <see cref="CurrencyOverlay"/>.
/// </summary>
public class CurrencyOverlayConfigNode : EntryConfigurationNode<CurrencySetting> {

    private readonly TextNode itemNameTextNode;
    private readonly IconImageNode iconImageNode;
    private readonly CheckboxNode enableLowLimitCheckbox;
    private readonly NumericInputNode lowLimitInputNode;
    private readonly CheckboxNode enableHighLimitCheckbox;
    private readonly NumericInputNode highLimitInputNode;
    private readonly CheckboxNode reverseIconCheckbox;
    private readonly CheckboxNode reverseTextCheckbox;
    private readonly CheckboxNode allowMovingCheckbox;
    private readonly FloatSliderNode scaleSliderNode;
    private readonly CheckboxNode fadeIfNoWarningsCheckbox;
    private readonly FloatSliderNode fadeSliderNode;
    private readonly TabbedVerticalListNode layoutNode;

    public CurrencyOverlayConfigNode() {
        iconImageNode = new IconImageNode {
            FitTexture = true,
            Alpha = 0.1f,
        };
        iconImageNode.AttachNode(ConfigurationContentNode);

        itemNameTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 18,
        };
        itemNameTextNode.AttachNode(ConfigurationContentNode);

        layoutNode = new TabbedVerticalListNode {
            ItemSpacing = 6.0f,
            FitWidth = true,
            NavIndex = 150,
            NavLeft = 1,
        };

        layoutNode.AddNode(0, enableLowLimitCheckbox = new CheckboxNode {
            Height = 24.0f,
            String = "Warn when below limit",
        });

        layoutNode.AddNode(1, lowLimitInputNode = new NumericInputNode {
            Height = 24.0f,
        });

        layoutNode.AddNode(0, enableHighLimitCheckbox = new CheckboxNode {
            Height = 24.0f,
            String = "Warn when above limit",
        });

        layoutNode.AddNode(1, highLimitInputNode = new NumericInputNode {
            Height = 24.0f,
        });

        layoutNode.AddNode(0, [
            reverseIconCheckbox = new CheckboxNode {
                Height = 24.0f,
                String = "Reverse icon position",
            },
            reverseTextCheckbox = new CheckboxNode {
                Height = 24.0f,
                String = "Reverse text position",
            },
            allowMovingCheckbox = new CheckboxNode {
                Height = 24.0f,
                String = "Enable moving overlay element",
            },
            new CategoryTextNode {
                Height = 24.0f,
                String = "Scale",
            },
            scaleSliderNode = new FloatSliderNode {
                Height = 24.0f,
                Min = 0.50f,
                Max = 3.00f,
            },
            fadeIfNoWarningsCheckbox = new CheckboxNode {
                Height = 24.0f,
                String = "Fade if no warnings",
            },
            new CategoryTextNode {
                Height = 24.0f,
                String = Strings.CurrencyOverlay_LabelFadePercentage,
            },
            fadeSliderNode = new FloatSliderNode {
                Height = 24.0f,
                Min = 0.0f,
                Max = 0.90f,
            },
        ]);

        layoutNode.AttachNode(ConfigurationContentNode);
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

    protected override void PopulateEntryData(CurrencySetting entry) {
        var itemInfo = Service<IDataManager>.Get().GetItem(entry.ItemId);

        iconImageNode.IconId = itemInfo.Icon;
        iconImageNode.IsVisible = iconImageNode.IconId is not 0;

        itemNameTextNode.String = itemInfo.Name.ToString();

        enableLowLimitCheckbox.OnClick = null;
        enableLowLimitCheckbox.IsChecked = entry.EnableLowLimit;
        enableLowLimitCheckbox.OnClick = newValue => {
            entry.EnableLowLimit = newValue;
            SaveConfig?.Invoke();
        };

        lowLimitInputNode.OnValueUpdate = null;
        lowLimitInputNode.Value = entry.LowLimit;
        lowLimitInputNode.OnValueUpdate = newValue => {
            entry.LowLimit = newValue;
            SaveConfig?.Invoke();
        };

        enableHighLimitCheckbox.OnClick = null;
        enableHighLimitCheckbox.IsChecked = entry.EnableHighLimit;
        enableHighLimitCheckbox.OnClick = newValue => {
            entry.EnableHighLimit = newValue;
            SaveConfig?.Invoke();
        };

        highLimitInputNode.OnValueUpdate = null;
        highLimitInputNode.Value = entry.HighLimit;
        highLimitInputNode.OnValueUpdate = newValue => {
            entry.HighLimit = newValue;
            SaveConfig?.Invoke();
        };

        reverseTextCheckbox.OnClick = null;
        reverseIconCheckbox.IsChecked = entry.IconReversed;
        reverseIconCheckbox.OnClick = newValue => {
            entry.IconReversed = newValue;
            SaveConfig?.Invoke();
        };

        reverseIconCheckbox.OnClick = null;
        reverseTextCheckbox.IsChecked = entry.TextReversed;
        reverseIconCheckbox.OnClick = newValue => {
            entry.TextReversed = newValue;
            SaveConfig?.Invoke();
        };

        allowMovingCheckbox.OnClick = null;
        allowMovingCheckbox.IsChecked = entry.IsNodeMoveable;
        allowMovingCheckbox.OnClick = newValue => {
            entry.IsNodeMoveable = newValue;
            SaveConfig?.Invoke();
        };

        scaleSliderNode.OnValueChanged = null;
        scaleSliderNode.Value = entry.Scale;
        scaleSliderNode.OnValueChanged = newValue => {
            entry.Scale = newValue;
            SaveConfig?.Invoke();
        };

        fadeIfNoWarningsCheckbox.OnClick = null;
        fadeIfNoWarningsCheckbox.IsChecked = entry.FadeIfNoWarnings;
        fadeIfNoWarningsCheckbox.OnClick = newValue => {
            entry.FadeIfNoWarnings = newValue;
            SaveConfig?.Invoke();
        };

        fadeSliderNode.OnValueChanged = null;
        fadeSliderNode.Value = entry.FadePercent;
        fadeSliderNode.OnValueChanged = newValue => {
            entry.FadePercent = newValue;
            SaveConfig?.Invoke();
        };
    }
}
