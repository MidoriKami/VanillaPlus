using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addons;
using KamiToolKit.Addons.Parts;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Widgets;
using KamiToolKit.Widgets.Parts;

namespace VanillaPlus.Features.WindowBackground;

public class WindowBackgroundConfigNode : ConfigNode<WindowBackgroundSetting> {

    private TabbedVerticalListNode verticalListNode;
    
    private TextNode windowNameTextNode;

    private HorizontalListNode colorPreviewLayoutNode;
    private ColorPreviewNode colorPreviewNode;
    private TextNode colorLabelNode;
    private Vector2EditWidget sizeEditWidget;

    private ColorPickerAddon colorPickerAddon;
    
    public WindowBackgroundConfigNode() {
        CollisionNode.IsVisible = false;
        
        colorPickerAddon = new ColorPickerAddon {
            NativeController = System.NativeController,
            InternalName = "WindowBackgroundColor",
            Title = "Window Background Color Picker",
            DefaultColor = KnownColor.Black.Vector() with { W = 0.66f },
        };
        
        windowNameTextNode = new TextNode {
            AlignmentType = AlignmentType.Center,
            FontSize = 18,
            IsVisible = true,
        };
        System.NativeController.AttachNode(windowNameTextNode, this);
        
        verticalListNode = new TabbedVerticalListNode {
            IsVisible = true,
            ItemVerticalSpacing = 20.0f,
        };
        verticalListNode.CollisionNode.IsVisible = false;
        System.NativeController.AttachNode(verticalListNode, this);

        verticalListNode.AddNode(new CategoryTextNode {
            String = "Background Color",
        });

        colorPreviewLayoutNode = new HorizontalListNode {
            Height = 32.0f,
            IsVisible = true,
            ItemSpacing = 10.0f,
        };
        verticalListNode.AddNode(1, colorPreviewLayoutNode);
        
        colorPreviewNode = new ColorPreviewNode {
            Size = new Vector2(32.0f, 32.0f),
            DrawFlags = DrawFlags.ClickableCursor,
            IsVisible = true,
        };
        colorPreviewLayoutNode.AddNode(colorPreviewNode);
        
        colorLabelNode = new TextNode {
            Size = new Vector2(100.0f, 32.0f),
            AlignmentType = AlignmentType.Left,
            FontType = FontType.Axis,
            FontSize = 14,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = "Color",
            DrawFlags = DrawFlags.ClickableCursor,
            IsVisible = true,
        };
        colorPreviewLayoutNode.AddNode(colorLabelNode);

        verticalListNode.AddNode(0, new CategoryTextNode {
            String = "Padding Size",
        });

        sizeEditWidget = new Vector2EditWidget {
            Height = 50.0f,
            IsVisible = true,
            OnValueChanged = newValue => {
                if (ConfigurationOption is not null) {
                    ConfigurationOption.Padding = newValue;
                    OptionChanged(ConfigurationOption);
                    OnConfigChanged?.Invoke(ConfigurationOption);
                }
            },
        };
        verticalListNode.AddNode(1, sizeEditWidget);

        colorPreviewLayoutNode.CollisionNode.DrawFlags |= DrawFlags.ClickableCursor;
        colorPreviewNode.CollisionNode.DrawFlags |= DrawFlags.ClickableCursor;
        colorPreviewLayoutNode.CollisionNode.AddEvent(AtkEventType.MouseClick, OpenColorPicker);
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

    private void OnAddonSearchResult(StringInfoNode selectedAddon) {
        if (ConfigurationOption is null) return;
        
        ConfigurationOption.AddonName = selectedAddon.Label;
        OptionChanged(ConfigurationOption);
        OnConfigChanged?.Invoke(ConfigurationOption);
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
