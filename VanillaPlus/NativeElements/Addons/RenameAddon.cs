using System;
using System.Numerics;
using System.Threading.Tasks;
using KamiToolKit;
using KamiToolKit.Nodes;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.NativeElements.Addons;

public class RenameAddon : NativeAddon {
    private TextInputNode? inputNode;
    private TextButtonNode? confirmButton;
    private TextButtonNode? cancelButton;

    public Action<ReadOnlySeString>? OnRenameComplete { get; set; }

    protected override Task BuildUiAsync() {
        SetWindowSize(325.0f, 115.0f);

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
            OnInputComplete = _ => OnConfirm(),
        };
        inputNode.AttachNode(this);

        var buttonSize = new Vector2(100.0f, 24.0f);
        var targetYPos = ContentSize.Y - buttonSize.Y + ContentStartPosition.Y;

        confirmButton = new TextButtonNode {
            Position = new Vector2(ContentStartPosition.X, targetYPos),
            Size = buttonSize,
            String = Strings.Common_Confirm,
            OnClick = OnConfirm,
        };
        confirmButton.AttachNode(this);

        cancelButton = new TextButtonNode {
            Position = new Vector2(ContentSize.X - buttonSize.X + ContentPadding.X, targetYPos),
            Size = buttonSize,
            String = Strings.Common_Cancel,
            OnClick = Close,
        };
        cancelButton.AttachNode(this);

        inputNode.SetFocus();

        return Task.CompletedTask;
    }

    private void OnConfirm() {
        if (inputNode is null) return;

        OnRenameComplete?.Invoke(inputNode.String);
        Close();
    }

    public string PlaceholderString { get; set; } = string.Empty;
    public string DefaultString { get; set; } = string.Empty;
    public bool AutoSelectAll { get; set; }
    public Func<ReadOnlySeString, bool>? IsInputValid { get; set; }
}
