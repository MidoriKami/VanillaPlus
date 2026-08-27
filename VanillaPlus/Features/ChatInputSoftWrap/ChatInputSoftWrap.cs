using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ChatInputSoftWrap;

public class ChatInputSoftWrap : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ChatInputSoftWrap,
        Description = Strings.ModificationDescription_ChatInputSoftWrap,
        Type = ModificationType.UserInterface,
        Authors = ["twelvehouse"],
        Tags = ["Chat"],
    };

    public override string ImageName => "ChatInputSoftWrap.png";

    private static readonly string[] PanelAddonNames = ["ChatLogPanel_0", "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"];

    private const uint BackgroundNodeId = 17;
    private const uint ImeCandidateNodeId = 4;
    private const uint MaxLine = 10;

    private AddonController<AddonChatLog>? chatLogController;
    private List<ChatLogPanelResizer>? panelResizers;

    private TextInputFlags2 savedFlags2;
    private TextFlags savedTextFlags;
    private uint savedMaxLine;

    private float baseInputY;
    private Vector2 baseInputSize;
    private float baseTextHeight;
    private float baseBackgroundHeight;
    private float baseCollisionHeight;
    private float baseChannelTextY;
    private bool captured;

    private float baseCandidateX;
    private float baseCandidateY;
    private bool candidateMoved;

    private int appliedDelta;

    public override async Task OnEnableAsync() {
        panelResizers = [];
        foreach (var addonName in PanelAddonNames) {
            var resizer = new ChatLogPanelResizer(addonName);
            panelResizers.Add(resizer);

            await resizer.EnableAsync();
        }

        unsafe {
            chatLogController = new AddonController<AddonChatLog> {
                AddonName = "ChatLog",
                OnSetup = SetupChatLog,
                OnPreUpdate = UpdateChatLog,
                OnFinalize = ResetChatLog,
            };
        }

        await chatLogController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        // Chat log first: it drives the updates that resize the panels.
        await chatLogController.DisposeAsyncSafe();
        chatLogController = null;

        if (panelResizers is not null) {
            foreach (var resizer in panelResizers) {
                await resizer.DisposeAsync();
            }
        }

        panelResizers = null;
        appliedDelta = 0;
    }

    private unsafe void SetupChatLog(AddonChatLog* addon) {
        var textInput = addon->TextInput;
        if (textInput is null) return;

        var textNode = ((AtkComponentInputBase*)textInput)->AtkTextNode;
        if (textNode is null) return;

        savedFlags2 = textInput->ComponentTextData.Flags2;
        savedTextFlags = textNode->TextFlags;
        savedMaxLine = textInput->ComponentTextData.MaxLine;

        CaptureStockGeometry(addon);

        // WordWrap alone wraps the display. MultiLine on the component would also let the user type
        // newlines, which chat cannot send, so it is left off.
        textInput->ComponentTextData.Flags2 = savedFlags2 | TextInputFlags2.WordWrap;
        textInput->ComponentTextData.MaxLine = MaxLine;
        textNode->TextFlags = savedTextFlags | TextFlags.WordWrap | TextFlags.MultiLine;
    }

    private unsafe void UpdateChatLog(AddonChatLog* addon) {
        if (!captured) return;

        var textInput = addon->TextInput;
        if (textInput is null) return;

        var textNode = ((AtkComponentInputBase*)textInput)->AtkTextNode;
        var inputNode = (AtkResNode*)((AtkComponentBase*)textInput)->OwnerNode;
        if (textNode is null || inputNode is null) return;

        // A height that was not written here means the game laid the chat window out again, so
        // what it left behind is the new stock geometry.
        if (Math.Abs(inputNode->Size.Y - (baseInputSize.Y + appliedDelta)) > float.Epsilon) {
            CaptureStockGeometry(addon);
        }

        var delta = MeasureExtraHeight(textNode);

        // Written every frame rather than on change: the game rewrites parts of this layout on its
        // own, and only the nodes it touches would be corrected otherwise, leaving the component
        // and its collision node disagreeing about where the input box ends.
        ApplyInputDelta(addon, delta);
        ApplyPanelDelta(delta);

        UpdateCandidateWindow(textInput);
    }

    private unsafe void ResetChatLog(AddonChatLog* addon) {
        ApplyInputDelta(addon, 0);
        captured = false;

        var textInput = addon->TextInput;
        if (textInput is null) return;

        var textNode = ((AtkComponentInputBase*)textInput)->AtkTextNode;
        if (textNode is not null) {
            textNode->TextFlags = savedTextFlags;
        }

        textInput->ComponentTextData.Flags2 = savedFlags2;
        textInput->ComponentTextData.MaxLine = savedMaxLine;

        if (candidateMoved) {
            var candidate = ((AtkComponentBase*)textInput)->GetNodeById(ImeCandidateNodeId);
            if (candidate is not null) {
                candidate->Position = new Vector2(baseCandidateX, baseCandidateY);
            }

            candidateMoved = false;
        }
    }

    private unsafe void CaptureStockGeometry(AddonChatLog* addon) {
        var textInput = addon->TextInput;
        if (textInput is null) return;

        var inputNode = (AtkResNode*)((AtkComponentBase*)textInput)->OwnerNode;
        var textNode = (AtkResNode*)((AtkComponentInputBase*)textInput)->AtkTextNode;
        if (inputNode is null || textNode is null) return;

        appliedDelta = 0;

        baseInputY = inputNode->Y;
        baseInputSize = inputNode->Size;
        baseTextHeight = textNode->Size.Y;

        var background = ((AtkComponentBase*)textInput)->GetNodeById(BackgroundNodeId);
        baseBackgroundHeight = background is null ? 0.0f : background->Size.Y;

        var collision = (AtkResNode*)((AtkComponentInputBase*)textInput)->CollisionNode;
        baseCollisionHeight = collision is null ? 0.0f : collision->Size.Y;

        var channelText = (AtkResNode*)addon->CurrentChannelTextNode;
        baseChannelTextY = channelText is null ? 0.0f : channelText->Y;

        captured = true;
    }

    /// <summary>
    /// Extra height the wrapped text needs beyond the original single line. The measurement follows
    /// from the node width only, so growing the node does not feed back into it.
    /// </summary>
    private static unsafe int MeasureExtraHeight(AtkTextNode* textNode) {
        ushort width = 0, height = 0;
        textNode->GetTextDrawSize(&width, &height);

        var spacing = Math.Max((ushort)1, textNode->LineSpacing);
        var lines = Math.Clamp((height + spacing / 2) / spacing, 1, (int)MaxLine);

        return (lines - 1) * spacing;
    }

    private unsafe void ApplyInputDelta(AddonChatLog* addon, int delta) {
        if (!captured) return;

        var textInput = addon->TextInput;
        if (textInput is null) return;

        var inputNode = (AtkResNode*)((AtkComponentBase*)textInput)->OwnerNode;
        var textNode = (AtkResNode*)((AtkComponentInputBase*)textInput)->AtkTextNode;
        if (inputNode is null || textNode is null) return;

        appliedDelta = delta;

        // Grows upward: the tab row sits directly below the input box.
        inputNode->Position = new Vector2(inputNode->X, baseInputY - delta);
        inputNode->Size = baseInputSize + new Vector2(0.0f, delta);
        textNode->Size = new Vector2(textNode->Size.X, baseTextHeight + delta);

        var background = ((AtkComponentBase*)textInput)->GetNodeById(BackgroundNodeId);
        if (background is not null) {
            background->Size = new Vector2(background->Size.X, baseBackgroundHeight + delta);
        }

        var collision = (AtkResNode*)((AtkComponentInputBase*)textInput)->CollisionNode;
        if (collision is not null) {
            collision->Size = new Vector2(collision->Size.X, baseCollisionHeight + delta);
        }

        // The channel name sits at the old top edge and would end up inside the grown box.
        var channelText = (AtkResNode*)addon->CurrentChannelTextNode;
        if (channelText is not null) {
            channelText->Position = new Vector2(channelText->X, baseChannelTextY - delta);
        }
    }

    private void ApplyPanelDelta(int delta) {
        if (panelResizers is null) return;

        foreach (var resizer in panelResizers) {
            resizer.Apply(delta);
        }
    }

    /// <summary>
    /// The game pins the IME candidate list above the top edge of the input, which is far from the
    /// caret once the box is several lines tall. Follow the caret instead.
    /// </summary>
    private unsafe void UpdateCandidateWindow(AtkComponentTextInput* textInput) {
        var candidate = ((AtkComponentBase*)textInput)->GetNodeById(ImeCandidateNodeId);
        if (candidate is null) return;

        if (!candidate->IsVisible()) {
            candidateMoved = false;
            return;
        }

        var cursor = ((AtkComponentInputBase*)textInput)->CursorContainer;
        if (cursor is null) return;

        if (!candidateMoved) {
            candidateMoved = true;
            baseCandidateX = candidate->X;
            baseCandidateY = candidate->Y;
        }

        var inputNode = (AtkResNode*)((AtkComponentBase*)textInput)->OwnerNode;
        var limit = inputNode is null ? 0.0f : inputNode->Size.X - candidate->Size.X;

        candidate->Position = new Vector2(
            Math.Clamp(cursor->X, 0.0f, Math.Max(0.0f, limit)),
            cursor->Y - candidate->Size.Y);
    }
}
