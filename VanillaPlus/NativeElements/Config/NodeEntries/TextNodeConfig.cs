using System;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addons;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Slider;
using KamiToolKit.Widgets.Parts;

namespace VanillaPlus.NativeElements.Config.NodeEntries;

public class TextNodeConfig : NodeConfig<TextNode> {

    private ColorPickerAddon? colorPickerAddon;

    private void InitializeColorPicker() {
        if (colorPickerAddon is not null) return;

        colorPickerAddon = new ColorPickerAddon {
            NativeController = System.NativeController,
            InternalName = "ColorPicker",
            Title = "Color Picker",
        };
    }

    public override void Dispose() {
        base.Dispose();
        
        colorPickerAddon?.Dispose();
        colorPickerAddon = null;
    }

    protected override SimpleComponentNode? BuildOption(NodeConfigEnum configOption) => configOption switch {
        NodeConfigEnum.TextColor => BuildTextColor(),
        NodeConfigEnum.TextOutlineColor => BuildTextOutlineColor(),
        NodeConfigEnum.TextSize => BuildTextSize(),
        NodeConfigEnum.TextFont => BuildTextFont(),
        NodeConfigEnum.TextAlignment => BuildTextAlignment(),

        _ => base.BuildOption(configOption),
    };

    private SimpleComponentNode? BuildTextColor() {
        if (StyleObject is null) return null;

        var container = new SimpleComponentNode {
            Height = 28.0f,
        };

        var labelNode = new LabelTextNode {
            String = "Text Color",
            Size = new Vector2(100.0f, 28.0f),
        };
        System.NativeController.AttachNode(labelNode, container);
        
        InitializeColorPicker();
        if (colorPickerAddon is null) return null;

        var colorPreviewNode = new ColorPreviewWithInput {
            Size = new Vector2(150.0f, 28.0f),
            Position = new Vector2(100.0f + 2.0f, 0.0f),
            Color = StyleObject.TextColor,
        };

        colorPreviewNode.OnColorChanged = vector4 => {
            StyleObject.TextColor = vector4;
            colorPreviewNode.Color = vector4;
            SaveStyleObject();
        };

        colorPreviewNode.ColorPreviewNode.CollisionNode.DrawFlags = DrawFlags.ClickableCursor;
        colorPreviewNode.ColorPreviewNode.CollisionNode.AddEvent(AtkEventType.MouseClick, () => {
            colorPickerAddon.InitialColor = StyleObject.TextColor;
            colorPickerAddon.OnColorConfirmed = vector4 => {
                StyleObject.TextColor = vector4;
                colorPreviewNode.Color = vector4;
                SaveStyleObject();
            };
            colorPickerAddon.Toggle();
        });
        
        System.NativeController.AttachNode(colorPreviewNode, container);
        
        return container;
    }

    private SimpleComponentNode? BuildTextOutlineColor() {
        if (StyleObject is null) return null;

        var container = new SimpleComponentNode {
            Height = 28.0f,
        };

        var labelNode = new LabelTextNode {
            String = "Text Outline",
            Size = new Vector2(100.0f, 28.0f),
        };
        System.NativeController.AttachNode(labelNode, container);
        
        InitializeColorPicker();
        if (colorPickerAddon is null) return null;
        
        var colorPreviewNode = new ColorPreviewWithInput {
            Size = new Vector2(150.0f, 28.0f),
            Position = new Vector2(100.0f + 2.0f, 0.0f),
            Color = StyleObject.TextOutlineColor,
        };

        colorPreviewNode.OnColorChanged = vector4 => {
            StyleObject.TextOutlineColor = vector4;
            colorPreviewNode.Color = vector4;
            SaveStyleObject();
        };

        colorPreviewNode.ColorPreviewNode.CollisionNode.DrawFlags = DrawFlags.ClickableCursor;
        colorPreviewNode.ColorPreviewNode.CollisionNode.AddEvent(AtkEventType.MouseClick, () => {
            colorPickerAddon.InitialColor = StyleObject.TextOutlineColor;
            colorPickerAddon.OnColorConfirmed = vector4 => {
                StyleObject.TextOutlineColor = vector4;
                colorPreviewNode.Color = vector4;
                SaveStyleObject();
            };
            colorPickerAddon.Toggle();
        });
        
        System.NativeController.AttachNode(colorPreviewNode, container);
        
        return container;
    }

    private LabelLayoutNode? BuildTextSize() {
        if (StyleObject is null) return null;
        
        var container = new LabelLayoutNode {
            Height = 28.0f,
            FillWidth = true,
        };

        var labelNode = new LabelTextNode {
            String = "Font Size",
            Size = new Vector2(100.0f, 28.0f),
        };
        container.AddNode(labelNode);

        var fontSlider = new SliderNode {
            Height = 28.0f,
            Range = 8..32,
            Value = (int) StyleObject.FontSize,
            OnValueChanged = newValue => {
                StyleObject.FontSize = (uint) newValue;
                SaveStyleObject();
            },
        };
        container.AddNode(fontSlider);
        
        return container;
    }

    private LabelLayoutNode? BuildTextFont() {
        if (StyleObject is null) return null;
        
        var container = new LabelLayoutNode {
            Height = 28.0f,
            FillWidth = true,
        };
        
        var labelNode = new LabelTextNode {
            String = "Font",
            Size = new Vector2(100.0f, 28.0f),
        };
        container.AddNode(labelNode);

        var dropdown = new TextDropDownNode {
            Height = 28.0f,
            MaxListOptions = 10,
            Options = Enum.GetValues<FontType>().Select(value => value.ToString()).ToList(),
            SelectedOption = StyleObject.FontType.ToString(),
            OnOptionSelected = newValue => {
                var enumValue = Enum.Parse<FontType>(newValue);
                StyleObject.FontType = enumValue;
                SaveStyleObject();
            },
        };
        container.AddNode(dropdown);

        return container;
    }

    private LabelLayoutNode? BuildTextAlignment() {
        if (StyleObject is null) return null;
        
        var container = new LabelLayoutNode {
            Height = 28.0f,
            FillWidth = true,
        };
        
        var labelNode = new LabelTextNode {
            String = "Alignment",
            Size = new Vector2(100.0f, 28.0f),
        };
        container.AddNode(labelNode);

        var dropdown = new TextDropDownNode {
            Height = 28.0f,
            MaxListOptions = 10,
            Options = Enum.GetValues<AlignmentType>().Select(value => value.ToString()).ToList(),
            SelectedOption = StyleObject.AlignmentType.ToString(),
            OnOptionSelected = newValue => {
                var enumValue = Enum.Parse<AlignmentType>(newValue);
                StyleObject.AlignmentType = enumValue;
                SaveStyleObject();
            },
        };
        container.AddNode(dropdown);

        return container;
    }
}
