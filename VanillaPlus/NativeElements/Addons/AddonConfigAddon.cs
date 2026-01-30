using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.NativeElements.Addons;

public class AddonConfigAddon : NativeAddon {
    private TextButtonNode? keybindEnableButtonNode;
    private TextNode? keybindTextNode;

    private KeybindConfigAddon? keybindAddon; 

    public required AddonConfig AddonConfig { get; init; }

    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        SetWindowSize(390.0f, 360.0f);

        keybindAddon = new KeybindConfigAddon {
            InternalName = "KeybindConfig",
            Title = Strings.AddonConfig_KeybindWindowTitle,
            InitialKeybind = AddonConfig.Keybind,
            OnKeybindChanged = OnKeybindChanged,
        };
        
        var keybindLabelNode = new UnderlinedTextNode {
            Position = ContentStartPosition + new Vector2(0.0f, 10.0f),
            Size = new Vector2(ContentSize.X, 24.0f),
            String = Strings.AddonConfig_KeybindLabel,
        };
        keybindLabelNode.AttachNode(this);

        keybindTextNode = new TextNode {
            Position = new Vector2(ContentStartPosition.X, keybindLabelNode.Bounds.Bottom + 5.0f),
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
        keybindTextNode.AttachNode(this);
        
        keybindEnableButtonNode = new TextButtonNode {
            Position = new Vector2(ContentStartPosition.X, keybindTextNode.Bounds.Bottom + 10.0f),
            Size = new Vector2(150.0f, 24.0f),
            String = AddonConfig.KeybindEnabled ? Strings.Common_Disable : Strings.Common_Enable,
            OnClick = OnKeybindToggleClicked,
        };
        keybindEnableButtonNode.AttachNode(this);

        var editKeybindButtonNode = new TextButtonNode {
            Size = new Vector2(150.0f, 24.0f),
            Position = new Vector2(ContentStartPosition.X + ContentSize.X - 150.0f, keybindTextNode.Bounds.Bottom + 10.0f),
            String = Strings.AddonConfig_ChangeKeybind,
            OnClick = keybindAddon.Toggle,
        };
        editKeybindButtonNode.AttachNode(this);

        var inputComboLabelNode = new UnderlinedTextNode {
            Position = new Vector2(ContentStartPosition.X - 2.0f, editKeybindButtonNode.Bounds.Bottom + 15.0f),
            Size = new Vector2(ContentSize.X, 24.0f),
            String = Strings.AddonConfig_WindowSizeLabel,
        };
        inputComboLabelNode.AttachNode(this);

        var windowSizeGridNode = new GridNode {
            Position = new Vector2(ContentStartPosition.X, inputComboLabelNode.Bounds.Bottom + 5.0f),
            Size = new Vector2(ContentSize.X, 50.0f),
            GridSize = new GridSize(2, 2),
        };
        windowSizeGridNode.AttachNode(this);

        var windowWidthTextNode = new TextNode {
            Size = windowSizeGridNode[0, 0].Size,
            AlignmentType = AlignmentType.Bottom,
            FontType = FontType.Axis,
            FontSize = 14,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = Strings.AddonConfig_WindowWidthLabel,
        };
        windowWidthTextNode.AttachNode(windowSizeGridNode[0, 0]);

        var windowHeightTextNode = new TextNode {
            Size = windowSizeGridNode[1, 0].Size,
            AlignmentType = AlignmentType.Bottom,
            FontType = FontType.Axis,
            FontSize = 14,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = Strings.AddonConfig_WindowHeightLabel,
        };
        windowHeightTextNode.AttachNode(windowSizeGridNode[1, 0]);
        
        var widthInputNode = new NumericInputNode {
            Size = windowSizeGridNode[0, 1].Size - new Vector2(4.0f, 4.0f),
            Position = new Vector2(2.0f, 2.0f),
            Value = (int) AddonConfig.WindowSize.X,
            OnValueUpdate = newValue => {
                AddonConfig.WindowSize = new Vector2(newValue, AddonConfig.WindowSize.Y);
                AddonConfig.Save();
            },
        };
        widthInputNode.AttachNode(windowSizeGridNode[0, 1]);
        
        var heightInputNode = new NumericInputNode {
            Size = windowSizeGridNode[1, 1].Size - new Vector2(4.0f, 4.0f),
            Position = new Vector2(2.0f, 2.0f),
            Value = (int) AddonConfig.WindowSize.Y,
            OnValueUpdate = newValue => {
                AddonConfig.WindowSize = new Vector2(AddonConfig.WindowSize.X, newValue);
                AddonConfig.Save();
            },
        };
        heightInputNode.AttachNode(windowSizeGridNode[1, 1]);

        var editNoteTextNode = new TextNode {
            Position = new Vector2(ContentStartPosition.X, windowSizeGridNode.Y + windowSizeGridNode.Height),
            Size = new Vector2(ContentSize.X, 40.0f),
            AlignmentType = AlignmentType.Center,
            FontSize = 12,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = Strings.AddonConfig_ReloadHint,
        };
        editNoteTextNode.AttachNode(this);

        var additionalOptionsTextNode = new UnderlinedTextNode {
            Size = new Vector2(ContentSize.X, 28.0f),
            Position = new Vector2(ContentStartPosition.X, editNoteTextNode.Bounds.Bottom),
            String = "Additional Options",
        };
        additionalOptionsTextNode.AttachNode(this);

        new CheckboxNode {
            Position = new Vector2(ContentStartPosition.X, additionalOptionsTextNode.Bounds.Bottom),
            Size = new Vector2(ContentSize.X, 24.0f),
            String = "Disable Keybind in Combat",
            IsChecked = AddonConfig.DisableInCombat,
            OnClick = newValue => {
                AddonConfig.DisableInCombat = newValue;
                AddonConfig.Save();
            },
        }.AttachNode(this);
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        keybindAddon?.Dispose();
        keybindAddon = null;
    }
    
    private void OnKeybindToggleClicked() {
        if (keybindEnableButtonNode is null) return;
        if (keybindTextNode is null) return;

        AddonConfig.KeybindEnabled = !AddonConfig.KeybindEnabled;
        keybindEnableButtonNode.String = AddonConfig.KeybindEnabled ? Strings.Common_Disable : Strings.Common_Enable;
        keybindTextNode.MultiplyColor = AddonConfig.KeybindEnabled ? new Vector3(1.0f, 1.0f, 1.0f) : new Vector3(0.5f, 0.5f, 0.5f);
        
        AddonConfig.Save();
    }

    private void OnKeybindChanged(Keybind newKeybind) {
        AddonConfig.Keybind = newKeybind;
        AddonConfig.Save();

        keybindTextNode?.String = AddonConfig.Keybind.ToString();
    }
}
