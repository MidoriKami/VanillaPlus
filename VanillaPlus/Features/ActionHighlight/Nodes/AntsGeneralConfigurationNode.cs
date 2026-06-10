using System;
using System.Numerics;
using System.Threading.Tasks;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.ActionHighlight.Nodes;

/// <summary>
/// Node used for displaying general configuration options for <see cref="ActionHighlight"/>.
/// </summary>
public class AntsGeneralConfigurationNode : ResNode {

    public AntsGeneralConfigurationNode() {
        if (ActionHighlight.Config is null) throw new Exception("Config is invalid somehow");

        settingsListNode = new TabbedVerticalListNode {
            FirstItemSpacing = 16.0f,
            ItemSpacing = 5.0f,
            InitialTabbedNodes = [
                new TabbedListEntry(1, new CategoryTextNode {
                    String = "General Settings",
                }),
                new TabbedListEntry(2, [
                    new CheckboxNode {
                        IsChecked = ActionHighlight.Config.ShowOnlyInCombat,
                        OnClick = value => {
                            ActionHighlight.Config.ShowOnlyInCombat = value;
                            Task.Run(ActionHighlight.Config.Save);
                        },
                        Size = new Vector2(300.0f, 24.0f),
                        String = Strings.ActionHighlight_ShowInCombat,
                    },
                    new CheckboxNode {
                        IsChecked = ActionHighlight.Config.AntOnlyOnFinalStack,
                        OnClick = value => {
                            ActionHighlight.Config.AntOnlyOnFinalStack = value;
                            Task.Run(ActionHighlight.Config.Save);
                        },
                        Size = new Vector2(300.0f, 24.0f),
                        String = Strings.ActionHighlight_AntOnlyOnFinalStack,
                    },
                    new CheckboxNode {
                        IsChecked = ActionHighlight.Config.ShowOnlyUsableActions,
                        OnClick = value => {
                            ActionHighlight.Config.ShowOnlyUsableActions = value;
                            Task.Run(ActionHighlight.Config.Save);
                        },
                        Size = new Vector2(300.0f, 24.0f),
                        String = Strings.ActionHighlight_ShowOnlyUsableActions,
                    },
                    new CheckboxNode {
                        IsChecked = ActionHighlight.Config.UseGlocalPreAntMs,
                        OnClick = value => {
                            ActionHighlight.Config.UseGlocalPreAntMs = value;
                            Task.Run(ActionHighlight.Config.Save);
                        },
                        Size = new Vector2(300.0f, 24.0f),
                        String = Strings.ActionHighlight_UseGlobalPreAntMs,
                    },
                ]),
                new TabbedListEntry(0, new ResNode()),
                new TabbedListEntry(1, new CategoryTextNode {
                    String = Strings.ActionHighlight_GlobalPreAntTimeMs,
                }),
                new TabbedListEntry(2, new NumericInputNode {
                    Value = ActionHighlight.Config.PreAntTimeMs,
                    OnValueUpdate = newValue => {
                        ActionHighlight.Config.PreAntTimeMs = newValue;
                        Task.Run(ActionHighlight.Config.Save);
                    },
                    Size = new Vector2(100.0f, 24.0f),
                }),
            ],
        };
        settingsListNode.AttachNode(this);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        settingsListNode.Size = Size;
        settingsListNode.RecalculateLayout();
    }

    private readonly TabbedVerticalListNode settingsListNode;
}
