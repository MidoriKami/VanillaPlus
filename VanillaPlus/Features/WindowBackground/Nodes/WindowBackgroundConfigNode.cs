using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Color;
using KamiToolKit.Premade.Nodes;
using KamiToolKit.Premade.Widgets;

namespace VanillaPlus.Features.WindowBackground.Nodes;

public class WindowBackgroundConfigNode : ConfigNode<WindowBackgroundSetting> {

    private readonly TabbedVerticalListNode verticalListNode;
    
    private readonly TextNode windowNameTextNode;

    private readonly ColorEditNode colorEditNode;
    private readonly Vector2EditWidget sizeEditWidget;

    public WindowBackgroundConfigNode() {
        CollisionNode.IsVisible = false;

        windowNameTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 18,
        };
        windowNameTextNode.AttachNode(this);
        
        verticalListNode = new TabbedVerticalListNode {
            ItemVerticalSpacing = 20.0f,
        };
        verticalListNode.CollisionNode.IsVisible = false;
        verticalListNode.AttachNode(this);

        verticalListNode.AddNode(new CategoryTextNode {
            String = Strings.WindowBackground_CategoryBackgroundColor,
        });

        colorEditNode = new ColorEditNode {
            Size = new Vector2(150.0f, 32.0f),
            Label = Strings.Color,
        };
        verticalListNode.AddNode(1, colorEditNode);

        verticalListNode.AddNode(0, new CategoryTextNode {
            String = Strings.WindowBackground_CategoryPaddingSize,
        });

        sizeEditWidget = new Vector2EditWidget {
            Height = 50.0f,
            OnValueChanged = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.Padding = newValue;
                    OptionChanged(ConfigurationOption);
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        verticalListNode.AddNode(1, sizeEditWidget);
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();
        
        windowNameTextNode.Size = new Vector2(Width, 24.0f);
        windowNameTextNode.Position = new Vector2(0.0f, 50.0f);

        verticalListNode.Width = Width;
        verticalListNode.Position = new Vector2(0.0f, Height / 3.0f);

        verticalListNode.FitWidth = true;
        verticalListNode.RecalculateLayout();
    }

    protected override void OptionChanged(WindowBackgroundSetting? option) {
        if (option is null) return;
        
        windowNameTextNode.String = option.AddonName;
        
        colorEditNode.CurrentColor = option.Color;
        colorEditNode.DefaultColor = KnownColor.Black.Vector() with { W = 50.0f };

        sizeEditWidget.Value = option.Padding;
    }
}
