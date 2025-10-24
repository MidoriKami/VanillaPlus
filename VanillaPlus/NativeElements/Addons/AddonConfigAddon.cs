﻿using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Addon;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.NativeElements.Addons;

public class AddonConfigAddon : NativeAddon {
    private LabelTextNode? inputComboLabelNode;
    private HorizontalLineNode? topLineNode;

    private LabelTextNode? keybindLabelNode;
    private HorizontalLineNode? keybindLineNode;
    private TextButtonNode? keybindEnableButtonNode;
    private TextNode? keybindTextNode;
    private TextButtonNode? editKeybindButtonNode;

    private GridNode? windowSizeGridNode;
    private TextNode? windowWidthTextNode;
    private TextNode? windowHeightTextNode;
    private NumericInputNode? widthInputNode;
    private NumericInputNode? heightInputNode;

    private TextNode? editNoteTextNode;

    private KeybindConfigAddon? keybindAddon; 

    public required AddonConfig AddonConfig { get; init; }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        SetWindowSize(390.0f, 300.0f);

        keybindAddon = new KeybindConfigAddon {
            NativeController = System.NativeController,
            InternalName = "KeybindConfig",
            Title = "Keybind Config Window",
            InitialKeybind = AddonConfig.Keybind,
            OnKeybindChanged = OnKeybindChanged,
        };
        
        keybindLabelNode = new LabelTextNode {
            AlignmentType = AlignmentType.Left,
            Position = ContentStartPosition + new Vector2(0.0f, 10.0f),
            String = "Keybind",
            IsVisible = true,
        };
        AttachNode(keybindLabelNode);

        keybindLineNode = new HorizontalLineNode {
            Position = new Vector2(ContentStartPosition.X - 2.0f, keybindLabelNode.Y + keybindLabelNode.Height),
            Size = new Vector2(95.0f, 2.0f),
            IsVisible = true,
        };
        AttachNode(keybindLineNode);

        keybindTextNode = new TextNode {
            Position = new Vector2(ContentStartPosition.X, keybindLineNode.Y + keybindLineNode.Height + 5.0f),
            Size = new Vector2(ContentSize.X, 35.0f),
            AlignmentType = AlignmentType.Center,
            FontSize = 14,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = AddonConfig.Keybind.ToString(),
            MultiplyColor = AddonConfig.KeybindEnabled ? new Vector3(1.0f, 1.0f, 1.0f) : new Vector3(0.5f, 0.5f, 0.5f),
        };
        AttachNode(keybindTextNode);
        
        keybindEnableButtonNode = new TextButtonNode {
            Position = new Vector2(ContentStartPosition.X, keybindTextNode.Y + keybindTextNode.Height + 10.0f),
            Size = new Vector2(150.0f, 24.0f),
            String = AddonConfig.KeybindEnabled ? "Disable" : "Enable",
            IsVisible = true,
            OnClick = OnKeybindToggleClicked,
        };
        AttachNode(keybindEnableButtonNode);

        editKeybindButtonNode = new TextButtonNode {
            Size = new Vector2(150.0f, 24.0f),
            Position = new Vector2(ContentStartPosition.X + ContentSize.X - 150.0f, keybindTextNode.Y + keybindTextNode.Height + 10.0f),
            String = "Change Keybind",
            IsVisible = true,
            OnClick = keybindAddon.Toggle,
        };
        AttachNode(editKeybindButtonNode);
        
        inputComboLabelNode = new LabelTextNode {
            AlignmentType = AlignmentType.Left,
            Position = new Vector2(ContentStartPosition.X - 2.0f, editKeybindButtonNode.Y + editKeybindButtonNode.Height + 15.0f),
            String = "Window Size",
            IsVisible = true,
        };
        AttachNode(inputComboLabelNode);

        topLineNode = new HorizontalLineNode {
            Position = new Vector2(ContentStartPosition.X - 2.0f, inputComboLabelNode.Y + inputComboLabelNode.Height),
            Size = new Vector2(125.0f, 2.0f),
            IsVisible = true,
        };
        AttachNode(topLineNode);

        windowSizeGridNode = new GridNode {
            Position = new Vector2(ContentStartPosition.X, topLineNode.Y + topLineNode.Height + 5.0f),
            Size = new Vector2(ContentSize.X, 50.0f),
            GridSize = new GridSize(2, 2),
            IsVisible = true,
        };
        AttachNode(windowSizeGridNode);

        windowWidthTextNode = new TextNode {
            Size = windowSizeGridNode[0, 0].Size,
            AlignmentType = AlignmentType.Bottom,
            FontType = FontType.Axis,
            FontSize = 14,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = "Width",
            IsVisible = true,
        };
        AttachNode(windowWidthTextNode, windowSizeGridNode[0, 0]);

        windowHeightTextNode = new TextNode {
            Size = windowSizeGridNode[1, 0].Size,
            AlignmentType = AlignmentType.Bottom,
            FontType = FontType.Axis,
            FontSize = 14,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = "Height",
            IsVisible = true,
        };
        AttachNode(windowHeightTextNode, windowSizeGridNode[1, 0]);
        
        widthInputNode = new NumericInputNode {
            Size = windowSizeGridNode[0, 1].Size - new Vector2(4.0f, 4.0f),
            Position = new Vector2(2.0f, 2.0f),
            IsVisible = true,
            Value = (int) AddonConfig.WindowSize.X,
            OnValueUpdate = newValue => {
                AddonConfig.WindowSize = new Vector2(newValue, AddonConfig.WindowSize.Y);
                AddonConfig.Save();
            },
        };
        AttachNode(widthInputNode, windowSizeGridNode[0, 1]);
        
        heightInputNode = new NumericInputNode {
            Size = windowSizeGridNode[1, 1].Size - new Vector2(4.0f, 4.0f),
            Position = new Vector2(2.0f, 2.0f),
            IsVisible = true,
            Value = (int) AddonConfig.WindowSize.Y,
            OnValueUpdate = newValue => {
                AddonConfig.WindowSize = new Vector2(AddonConfig.WindowSize.X, newValue);
                AddonConfig.Save();
            },
        };
        AttachNode(heightInputNode, windowSizeGridNode[1, 1]);

        editNoteTextNode = new TextNode {
            Position = new Vector2(ContentStartPosition.X, windowSizeGridNode.Y + windowSizeGridNode.Height),
            Size = new Vector2(ContentSize.X, 40.0f),
            AlignmentType = AlignmentType.Center,
            FontSize = 12,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = "Changes won't take effect until the window is reopened",
        };
        AttachNode(editNoteTextNode);
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        keybindAddon?.Dispose();
        keybindAddon = null;
    }
    
    private void OnKeybindToggleClicked() {
        if (keybindEnableButtonNode is null) return;
        if (keybindTextNode is null) return;

        AddonConfig.KeybindEnabled = !AddonConfig.KeybindEnabled;
        keybindEnableButtonNode.String = AddonConfig.KeybindEnabled ? "Disable" : "Enable";
        keybindTextNode.MultiplyColor = AddonConfig.KeybindEnabled ? new Vector3(1.0f, 1.0f, 1.0f) : new Vector3(0.5f, 0.5f, 0.5f);
        
        AddonConfig.Save();
    }

    private void OnKeybindChanged(Keybind newKeybind) {
        AddonConfig.Keybind = newKeybind;
        AddonConfig.Save();

        if (keybindTextNode is not null) {
            keybindTextNode.String = AddonConfig.Keybind.ToString();
        }
    }
}
