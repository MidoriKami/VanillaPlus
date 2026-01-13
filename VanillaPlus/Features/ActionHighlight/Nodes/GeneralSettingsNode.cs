using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.ActionHighlight.Nodes;

public sealed class GeneralSettingsNode : SimpleComponentNode {
    private readonly ActionHighlightConfig config;
    private readonly VerticalListNode settingsListNode;

    public GeneralSettingsNode(ActionHighlightConfig config) {
        this.config = config;

        settingsListNode = new VerticalListNode {
            FirstItemSpacing = 35.0f,
            ItemSpacing = 5.0f,
        };
        settingsListNode.AttachNode(this);

        AddCheckbox(Strings.ActionHighlight_ShowInCombat, config.ShowOnlyInCombat, value => { config.ShowOnlyInCombat = value; config.Save(); });
        AddCheckbox(Strings.ActionHighlight_AntOnlyOnFinalStack, config.AntOnlyOnFinalStack, value => { config.AntOnlyOnFinalStack = value; config.Save(); });
        AddCheckbox(Strings.ActionHighlight_ShowOnlyUsableActions, config.ShowOnlyUsableActions, value => { config.ShowOnlyUsableActions = value; config.Save(); });
        AddCheckbox(Strings.ActionHighlight_UseGlobalPreAntMs, config.UseGlocalPreAntMs, value => { config.UseGlocalPreAntMs = value; config.Save(); });

        var preAntLabel = new TextNode {
            String = Strings.ActionHighlight_GlobalPreAntTimeMs,
            FontSize = 14,
            Height = 32.0f,
            AlignmentType = AlignmentType.BottomLeft,
        };
        settingsListNode.AddNode(preAntLabel);

        var preAntInput = new NumericInputNode {
            Value = config.PreAntTimeMs,
            OnValueUpdate = OnValueUpdate,
            Size = new Vector2(100.0f, 24.0f),
        };
        settingsListNode.AddNode(preAntInput);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        settingsListNode.Size = Size;
    }

    private void AddCheckbox(string label, bool initialValue, System.Action<bool> onChanged)
        => settingsListNode.AddNode(new CheckboxNode {
            IsChecked = initialValue,
            OnClick = onChanged,
            Size = new Vector2(300.0f, 24.0f),
            String = label,
        });

    private void OnValueUpdate(int newValue) {
        config.PreAntTimeMs = newValue;
        config.Save();
    }
}
