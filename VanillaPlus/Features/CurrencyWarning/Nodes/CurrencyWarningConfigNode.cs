using System;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Components.ConfigurationNodes;
using KamiToolKit.Nodes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.CurrencyWarning.Nodes;

public class CurrencyWarningConfigNode : EntryConfigurationNode<CurrencyWarningSetting> {
    private readonly TextNode itemNameTextNode;
    private readonly IconImageNode iconImageNode;
    private readonly MultiStateButtonNode<WarningMode> modeStateButton;
    private readonly NumericInputNode limitInput;
    private readonly TabbedVerticalListNode optionsContainer;

    public CurrencyWarningConfigNode() {
        iconImageNode = new IconImageNode {
            FitTexture = true,
            Alpha = 0.1f,
        };
        iconImageNode.AttachNode(ConfigurationContentNode);

        itemNameTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 20,
        };
        itemNameTextNode.AttachNode(ConfigurationContentNode);

        optionsContainer = new TabbedVerticalListNode {
            ItemSpacing = 10.0f,
            TabSize = 25.0f,
            InitialTabbedNodes = [
                new TabbedListEntry(0, modeStateButton = new MultiStateButtonNode<WarningMode> {
                    Size = new Vector2(200.0f, 24.0f),
                    States = Enum.GetValues<WarningMode>().ToList(),
                }),
                new TabbedListEntry(1, limitInput = new NumericInputNode {
                    Size = new Vector2(160.0f, 24.0f),
                }),
            ],
        };
        optionsContainer.AttachNode(ConfigurationContentNode);
    }

    protected override void PopulateEntryData(CurrencyWarningSetting entry) {
        var item = IDataManager.Get().GetItem(entry.ItemId);
        itemNameTextNode.String = item.Name.ToString();
        iconImageNode.IconId = item.Icon;

        modeStateButton.OnStateChanged = null;
        modeStateButton.SelectedState = entry.Mode;
        modeStateButton.OnStateChanged = newState => {
            entry.Mode = newState;
            SaveConfig?.Invoke();
        };

        limitInput.OnValueUpdate = null;
        limitInput.Max = (int)item.StackSize;
        limitInput.Value = entry.Limit;
        limitInput.OnValueUpdate = newValue => {
            entry.Limit = newValue;
            SaveConfig?.Invoke();
        };
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
