using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Nodes;

namespace VanillaPlus.NativeElements.Addons;

public class RenameAddon : NativeAddon {
    private TextInputNode? inputNode;
    private TextButtonNode? confirmButton;
    private TextButtonNode? cancelButton;

    public Action<string>? OnRenameComplete { get; set; }
    
    protected override unsafe void OnSetup(AtkUnitBase* addon) {
        SetWindowSize(250.0f, 125.0f);

        inputNode = new TextInputNode {
            Position = ContentStartPosition + new Vector2(0.0f, ContentPadding.Y),
            Size = new Vector2(ContentSize.X, 28.0f),
            PlaceholderString = PlaceholderString,
            String = DefaultString,
            AutoSelectAll = AutoSelectAll,
            OnInputReceived = s => {
                if (IsInputValid is not null) {
                    inputNode!.IsError = !IsInputValid(s.ToString());
                }
            },
        };
        inputNode.AttachNode(this);

        var buttonSize = new Vector2(100.0f, 24.0f);
        var targetYPos = ContentSize.Y - buttonSize.Y + ContentStartPosition.Y;
        
        confirmButton = new TextButtonNode {
            Position = new Vector2(ContentStartPosition.X, targetYPos),
            Size = buttonSize,
            String = "Confirm",
            OnClick = () => {
                OnRenameComplete?.Invoke(inputNode.String);
                Close();
            },
        };
        confirmButton.AttachNode(this);

        cancelButton = new TextButtonNode {
            Position = new Vector2(ContentSize.X - buttonSize.X + ContentPadding.X, targetYPos),
            Size = buttonSize,
            String = "Cancel",
            OnClick = Close,
        };
        cancelButton.AttachNode(this);
    }

    public string PlaceholderString { get; set; } = string.Empty;
    public string DefaultString { get; set; } = string.Empty;
    public bool AutoSelectAll { get; set; }
    public Func<string, bool>? IsInputValid { get; set; }
}
