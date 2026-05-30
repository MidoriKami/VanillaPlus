using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node.Simple;

namespace VanillaPlus.Features.ActionHighlight.Nodes;

public sealed class GeneralSettingsNode : SimpleComponentNode {
    private readonly VerticalListNode settingsListNode;

    public GeneralSettingsNode(ActionHighlightConfig config) {
        settingsListNode = new VerticalListNode {
            FirstItemSpacing = 35.0f,
            ItemSpacing = 5.0f,
            InitialNodes = [
                new CheckboxNode {
                    IsChecked = config.ShowOnlyInCombat,
                    OnClick = value => {
                        config.ShowOnlyInCombat = value;
                        Task.Run(config.Save);
                    },
                    Size = new Vector2(300.0f, 24.0f),
                    String = Strings.ActionHighlight_ShowInCombat,
                },
                new CheckboxNode {
                    IsChecked = config.AntOnlyOnFinalStack,
                    OnClick = value => {
                        config.AntOnlyOnFinalStack = value;
                        Task.Run(config.Save);
                    },
                    Size = new Vector2(300.0f, 24.0f),
                    String = Strings.ActionHighlight_AntOnlyOnFinalStack,
                },
                new CheckboxNode {
                    IsChecked = config.ShowOnlyUsableActions,
                    OnClick = value => {
                        config.ShowOnlyUsableActions = value;
                        Task.Run(config.Save);
                    },
                    Size = new Vector2(300.0f, 24.0f),
                    String = Strings.ActionHighlight_ShowOnlyUsableActions,
                },
                new CheckboxNode {
                    IsChecked = config.UseGlocalPreAntMs,
                    OnClick = value => {
                        config.UseGlocalPreAntMs = value;
                        Task.Run(config.Save);
                    },
                    Size = new Vector2(300.0f, 24.0f),
                    String = Strings.ActionHighlight_UseGlobalPreAntMs,
                },
                new TextNode {
                    String = Strings.ActionHighlight_GlobalPreAntTimeMs,
                    FontSize = 14,
                    Height = 32.0f,
                    AlignmentType = AlignmentType.BottomLeft,
                },
                new NumericInputNode {
                    Value = config.PreAntTimeMs,
                    OnValueUpdate = newValue => {
                        config.PreAntTimeMs = newValue;
                        Task.Run(config.Save);
                    },
                    Size = new Vector2(100.0f, 24.0f),
                },
            ],
        };
        settingsListNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        settingsListNode.Size = Size;
    }
}
