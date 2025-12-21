using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.NativeElements.Addons;

public class AddonConfigAddon : NativeAddon {
    private CategoryTextNode? inputComboLabelNode;
    private HorizontalLineNode? topLineNode;

    private CategoryTextNode? keybindLabelNode;
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
            InternalName = "KeybindConfig",
            Title = Strings("AddonConfig_KeybindWindowTitle"),
            InitialKeybind = AddonConfig.Keybind,
            OnKeybindChanged = OnKeybindChanged,
        };
        
        keybindLabelNode = new CategoryTextNode {
            AlignmentType = AlignmentType.Left,
            Position = ContentStartPosition + new Vector2(0.0f, 10.0f),
            String = Strings("AddonConfig_KeybindLabel"),
        };
        keybindLabelNode.AttachNode(this);

        keybindLineNode = new HorizontalLineNode {
            Position = new Vector2(ContentStartPosition.X - 2.0f, keybindLabelNode.Y + keybindLabelNode.Height),
            Size = new Vector2(95.0f, 2.0f),
        };
        keybindLineNode.AttachNode(this);

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
        keybindTextNode.AttachNode(this);
        
        keybindEnableButtonNode = new TextButtonNode {
            Position = new Vector2(ContentStartPosition.X, keybindTextNode.Y + keybindTextNode.Height + 10.0f),
            Size = new Vector2(150.0f, 24.0f),
            String = AddonConfig.KeybindEnabled ? Strings("Common_Disable") : Strings("Common_Enable"),
            OnClick = OnKeybindToggleClicked,
        };
        keybindEnableButtonNode.AttachNode(this);

        editKeybindButtonNode = new TextButtonNode {
            Size = new Vector2(150.0f, 24.0f),
            Position = new Vector2(ContentStartPosition.X + ContentSize.X - 150.0f, keybindTextNode.Y + keybindTextNode.Height + 10.0f),
            String = Strings("AddonConfig_ChangeKeybind"),
            OnClick = keybindAddon.Toggle,
        };
        editKeybindButtonNode.AttachNode(this);
        
        inputComboLabelNode = new CategoryTextNode {
            AlignmentType = AlignmentType.Left,
            Position = new Vector2(ContentStartPosition.X - 2.0f, editKeybindButtonNode.Y + editKeybindButtonNode.Height + 15.0f),
            String = Strings("AddonConfig_WindowSizeLabel"),
        };
        inputComboLabelNode.AttachNode(this);

        topLineNode = new HorizontalLineNode {
            Position = new Vector2(ContentStartPosition.X - 2.0f, inputComboLabelNode.Y + inputComboLabelNode.Height),
            Size = new Vector2(125.0f, 2.0f),
        };
        topLineNode.AttachNode(this);

        windowSizeGridNode = new GridNode {
            Position = new Vector2(ContentStartPosition.X, topLineNode.Y + topLineNode.Height + 5.0f),
            Size = new Vector2(ContentSize.X, 50.0f),
            GridSize = new GridSize(2, 2),
        };
        windowSizeGridNode.AttachNode(this);

        windowWidthTextNode = new TextNode {
            Size = windowSizeGridNode[0, 0].Size,
            AlignmentType = AlignmentType.Bottom,
            FontType = FontType.Axis,
            FontSize = 14,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = Strings("AddonConfig_WindowWidthLabel"),
        };
        windowWidthTextNode.AttachNode(windowSizeGridNode[0, 0]);

        windowHeightTextNode = new TextNode {
            Size = windowSizeGridNode[1, 0].Size,
            AlignmentType = AlignmentType.Bottom,
            FontType = FontType.Axis,
            FontSize = 14,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = Strings("AddonConfig_WindowHeightLabel"),
        };
        windowHeightTextNode.AttachNode(windowSizeGridNode[1, 0]);
        
        widthInputNode = new NumericInputNode {
            Size = windowSizeGridNode[0, 1].Size - new Vector2(4.0f, 4.0f),
            Position = new Vector2(2.0f, 2.0f),
            Value = (int) AddonConfig.WindowSize.X,
            OnValueUpdate = newValue => {
                AddonConfig.WindowSize = new Vector2(newValue, AddonConfig.WindowSize.Y);
                AddonConfig.Save();
            },
        };
        widthInputNode.AttachNode(windowSizeGridNode[0, 1]);
        
        heightInputNode = new NumericInputNode {
            Size = windowSizeGridNode[1, 1].Size - new Vector2(4.0f, 4.0f),
            Position = new Vector2(2.0f, 2.0f),
            Value = (int) AddonConfig.WindowSize.Y,
            OnValueUpdate = newValue => {
                AddonConfig.WindowSize = new Vector2(AddonConfig.WindowSize.X, newValue);
                AddonConfig.Save();
            },
        };
        heightInputNode.AttachNode(windowSizeGridNode[1, 1]);

        editNoteTextNode = new TextNode {
            Position = new Vector2(ContentStartPosition.X, windowSizeGridNode.Y + windowSizeGridNode.Height),
            Size = new Vector2(ContentSize.X, 40.0f),
            AlignmentType = AlignmentType.Center,
            FontSize = 12,
            LineSpacing = 14,
            TextColor = ColorHelper.GetColor(8),
            TextOutlineColor = ColorHelper.GetColor(7),
            TextFlags = TextFlags.Edge | TextFlags.AutoAdjustNodeSize,
            String = Strings("AddonConfig_ReloadHint"),
        };
        editNoteTextNode.AttachNode(this);
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon) {
        keybindAddon?.Dispose();
        keybindAddon = null;
    }
    
    private void OnKeybindToggleClicked() {
        if (keybindEnableButtonNode is null) return;
        if (keybindTextNode is null) return;

        AddonConfig.KeybindEnabled = !AddonConfig.KeybindEnabled;
        keybindEnableButtonNode.String = AddonConfig.KeybindEnabled ? Strings("Common_Disable") : Strings("Common_Enable");
        keybindTextNode.MultiplyColor = AddonConfig.KeybindEnabled ? new Vector3(1.0f, 1.0f, 1.0f) : new Vector3(0.5f, 0.5f, 0.5f);
        
        AddonConfig.Save();
    }

    private void OnKeybindChanged(Keybind newKeybind) {
        AddonConfig.Keybind = newKeybind;
        AddonConfig.Save();

        keybindTextNode?.String = AddonConfig.Keybind.ToString();
    }
}
