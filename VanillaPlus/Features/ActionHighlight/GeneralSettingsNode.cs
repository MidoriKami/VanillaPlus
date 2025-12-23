using System.Numerics;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;

namespace VanillaPlus.Features.ActionHighlight;

public sealed class GeneralSettingsNode : SimpleComponentNode {
    private readonly ActionHighlightConfig config;

    public GeneralSettingsNode(ActionHighlightConfig config) {
        this.config = config;

        var y = 0.0f;

        AddCheckbox("Show Only In Combat", config.ShowOnlyInCombat, value => { config.ShowOnlyInCombat = value; config.Save(); }, ref y);
        AddCheckbox("Ant Only On Final Stack", config.AntOnlyOnFinalStack, value => { config.AntOnlyOnFinalStack = value; config.Save(); }, ref y);
        AddCheckbox("Show Only Usable Actions", config.ShowOnlyUsableActions, value => { config.ShowOnlyUsableActions = value; config.Save(); }, ref y);
        AddCheckbox("Use Global Pre-Ant Ms", config.UseGlocalPreAntMs, value => { config.UseGlocalPreAntMs = value; config.Save(); }, ref y);

        var preAntInput = new NumericInputNode {
            Value = config.PreAntTimeMs,
            OnValueUpdate = value => { config.PreAntTimeMs = value; config.Save(); },
            Size = new Vector2(100.0f, 24.0f),
            Position = new Vector2(10.0f, y),
        };
        preAntInput.AttachNode(this);

        var preAntLabel = new TextNode {
            String = "Global Pre-Ant Time (ms)",
            FontSize = 14,
            Position = new Vector2(120.0f, y + 4.0f),
        };
        preAntLabel.AttachNode(this);

        y += 30.0f;

        Height = y;
    }

    private void AddCheckbox(string label, bool initialValue, global::System.Action<bool> onChanged, ref float y) {
        var checkbox = new CheckboxNode {
            IsChecked = initialValue,
            OnClick = onChanged,
            Position = new Vector2(10.0f, y),
            Size = new Vector2(24.0f, 24.0f),
        };
        checkbox.AttachNode(this);

        var text = new TextNode {
            String = label,
            FontSize = 14,
            Position = new Vector2(40.0f, y + 4.0f),
        };
        text.AttachNode(this);

        y += 30.0f;
    }
}

