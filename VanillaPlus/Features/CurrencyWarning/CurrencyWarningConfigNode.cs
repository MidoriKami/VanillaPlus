using System;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;

namespace VanillaPlus.Features.CurrencyWarning;

public class CurrencyWarningConfigNode : ConfigNode<CurrencyWarningSetting> {
    private readonly TextNode itemNameTextNode;
    private readonly IconImageNode iconImageNode;

    private readonly MultiStateButtonNode<WarningMode> modeStateButton;
    private readonly NumericInputNode limitInput;

    private readonly TabbedVerticalListNode optionsContainer;

    private bool isUpdating;

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

        modeStateButton = new MultiStateButtonNode<WarningMode> {
            Size = new Vector2(200.0f, 24.0f),
            States = Enum.GetValues<WarningMode>().ToList(),
            OnStateChanged = newIndex => {
                if (isUpdating) return;

                if (ConfigurationOption is not null) {
                    ConfigurationOption.Mode = newIndex;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        optionsContainer.AddNode(modeStateButton);

        limitInput = new NumericInputNode {
            Size = new Vector2(160.0f, 24.0f),
            OnValueUpdate = newValue => {
                if (isUpdating) return;

                if (ConfigurationOption is not null) {
                    ConfigurationOption.Limit = newValue;
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        optionsContainer.AddNode(1, limitInput);
    }

    protected override void OptionChanged(CurrencyWarningSetting? option) {
        if (option is null) return;

        isUpdating = true;

        var item = Services.DataManager.GetItem(option.ItemId);
        itemNameTextNode.String = option.GetLabel();
        iconImageNode.IconId = item.Icon;

        modeStateButton.SelectedState = option.Mode;
        limitInput.Max = (int)item.StackSize;
        limitInput.Value = option.Limit;

        optionsContainer.RecalculateLayout();

        isUpdating = false;
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
