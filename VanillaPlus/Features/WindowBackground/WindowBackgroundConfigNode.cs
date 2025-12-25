using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Addons;
using KamiToolKit.Premade.Nodes;
using KamiToolKit.Premade.Widgets;

namespace VanillaPlus.Features.WindowBackground;

public class WindowBackgroundConfigNode : ConfigNode<WindowBackgroundSetting> {

    private readonly TabbedVerticalListNode verticalListNode;
    
    private readonly TextNode windowNameTextNode;

    private readonly ColorPreviewNode colorPreviewNode;
    private readonly Vector2EditWidget sizeEditWidget;

    private readonly ColorPickerAddon colorPickerAddon;
    
    public WindowBackgroundConfigNode() {
        CollisionNode.IsVisible = false;

        colorPickerAddon = new ColorPickerAddon {
            InternalName = "WindowBackgroundColor",
            Title = Strings("WindowBackground_ColorPickerTitle"),
            DefaultColor = KnownColor.Black.Vector() with { W = 0.50f },
            OnHsvaColorPreviewed = color => colorPreviewNode?.HsvaColor = color,
        };
        
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
            String = Strings("WindowBackground_CategoryBackgroundColor"),
        });

        var horizontalLayoutNode = new HorizontalListNode {
            Height = 32.0f,
            ItemSpacing = 10.0f,
        };
        verticalListNode.AddNode(1, horizontalLayoutNode);
        
        colorPreviewNode = new ColorPreviewNode {
            Size = new Vector2(32.0f, 32.0f),
            DrawFlags = DrawFlags.ClickableCursor,
        };
        horizontalLayoutNode.AddNode(colorPreviewNode);
        
        var colorLabelNode1 = new TextNode {
            Size = new Vector2(100.0f, 32.0f),
            AlignmentType = AlignmentType.Left,
            FontType = FontType.Axis,
            FontSize = 14,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = Strings("Color"),
            DrawFlags = DrawFlags.ClickableCursor,
        };
        horizontalLayoutNode.AddNode(colorLabelNode1);

        verticalListNode.AddNode(0, new CategoryTextNode {
            String = Strings("WindowBackground_CategoryPaddingSize"),
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

        horizontalLayoutNode.CollisionNode.DrawFlags |= DrawFlags.ClickableCursor;
        colorPreviewNode.CollisionNode.DrawFlags |= DrawFlags.ClickableCursor;
        horizontalLayoutNode.CollisionNode.AddEvent(AtkEventType.MouseClick, OpenColorPicker);
        colorPreviewNode.CollisionNode.AddEvent(AtkEventType.MouseClick, OpenColorPicker);
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
        
        colorPreviewNode.Color = option.Color;
        colorPickerAddon.InitialColor = option.Color;
        sizeEditWidget.Value = option.Padding;
    }

    private void OpenColorPicker() {
        colorPickerAddon.OnColorConfirmed = newColor => {
            if (ConfigurationOption is not null) {
                ConfigurationOption.Color = newColor;
                OptionChanged(ConfigurationOption);
                OnConfigChanged?.Invoke(ConfigurationOption);
            }
        };
                
        colorPickerAddon.Toggle();
    }
}
