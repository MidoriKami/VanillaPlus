using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.ActionHighlight;

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
        
        AddCheckbox("Show Only In Combat", config.ShowOnlyInCombat, value => { config.ShowOnlyInCombat = value; config.Save(); });
        AddCheckbox("Ant Only On Final Stack", config.AntOnlyOnFinalStack, value => { config.AntOnlyOnFinalStack = value; config.Save(); });
        AddCheckbox("Show Only Usable Actions", config.ShowOnlyUsableActions, value => { config.ShowOnlyUsableActions = value; config.Save(); });
        AddCheckbox("Use Global Pre-Ant Ms", config.UseGlocalPreAntMs, value => { config.UseGlocalPreAntMs = value; config.Save(); });

        var preAntLabel = new TextNode {
            String = "Global Pre-Ant Time (ms)",
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

    private void AddCheckbox(string label, bool initialValue, global::System.Action<bool> onChanged)
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
