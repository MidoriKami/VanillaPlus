using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;

namespace VanillaPlus.Native.Addons;

public class SeasonEventAddon : NativeAddon {
    public SeasonEventAddon() {
        // Always appear in the center of the screen
        RememberClosePosition = false;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
        base.OnSetup(addon, atkValueSpan);

        var iconSize = new Vector2(320.0f, 320.0f);

        var containerNode = new SimpleComponentNode {
            Position = ContentStartPosition,
            Size = ContentSize,
            NodeFlags = NodeFlags.Clip | NodeFlags.Visible,
        };
        containerNode.AttachNode(this);

        new IconImageNode {
            Size = iconSize,
            Origin = iconSize / 2.0f,
            FitTexture = true,
            IconId = 230317,
            Position = new Vector2(ContentSize.X - iconSize.X, ContentStartPosition.Y - 64.0f),
            Alpha = 0.10f,
        }.AttachNode(containerNode);

        new IconImageNode {
            Size = iconSize,
            Origin = iconSize / 2.0f,
            FitTexture = true,
            IconId = 230317,
            Position = new Vector2(-64.0f, ContentStartPosition.Y),
            RotationDegrees = -45.0f,
            Alpha = 0.10f,
        }.AttachNode(containerNode);

        new VerticalListNode {
            Size = new Vector2(ContentSize.X, 256.0f),
            Position = ContentStartPosition,
            Anchor = VerticalListAnchor.Top,
            FitContents = true,
            FitWidth = true,
            InitialNodes = [
                new TextNode {
                    Height = 64.0f,
                    String = Strings.SeasonEventNotice_Heading,
                    FontSize = 32,
                    AlignmentType = AlignmentType.Center,
                },
                new TextNode {
                    Height = 32.0f,
                    String = Strings.SeasonEventNotice_Body,
                    LineSpacing = 20,
                    AlignmentType = AlignmentType.Center,
                    TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
                },
                new CategoryTextNode {
                    Height = 32.0f,
                    String = Strings.SeasonEventNotice_DailyHint,
                    AlignmentType = AlignmentType.Bottom,
                },
                new ResNode { Height = 32.0f },
                new HorizontalFlexNode {
                    Height = 28.0f,
                    AlignmentFlags = FlexFlags.FitHeight | FlexFlags.CenterHorizontally,
                    InitialNodes = [
                        new TextButtonNode {
                            Width = 200.0f,
                            String = Strings.SeasonEventNotice_ButtonDismiss,
                            OnClick = Close,
                        },
                        new TextButtonNode {
                            Width = 200.0f,
                            String = Strings.SeasonEventNotice_ButtonOpen,
                            OnClick = System.ModificationBrowserAddon.Toggle,
                        },
                    ],
                },
            ],
        }.AttachNode(this);
    }
}
