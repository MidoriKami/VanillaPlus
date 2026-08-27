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
    private const uint MaxLine = 20;

    private AddonController<AddonChatLog>? chatLogController;
    private List<ChatLogPanelResizer>? panelResizers;

    private int currentDelta;
    private float lastTextWidth;

    private float textHeightGap;
    private float channelTextGap;
    private bool gapsKnown;

    private TextInputFlags2 savedFlags2;
    private TextFlags savedTextFlags;
    private uint savedMaxLine;

    private float baseCandidateX;
    private float baseCandidateY;
    private bool candidateMoved;

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
                OnDraw = ReapplyInputDelta,
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
    }

    private unsafe void SetupChatLog(AddonChatLog* addon) {
        var textInput = addon->TextInput;
        if (textInput is null) return;

        var textNode = ((AtkComponentInputBase*)textInput)->AtkTextNode;
        if (textNode is null) return;

        savedFlags2 = textInput->ComponentTextData.Flags2;
        savedTextFlags = textNode->TextFlags;
        savedMaxLine = textInput->ComponentTextData.MaxLine;

        // WordWrap alone wraps the display. MultiLine on the component would also let the user type
        // newlines, which chat cannot send, so it is left off.
        textInput->ComponentTextData.Flags2 = savedFlags2 | TextInputFlags2.WordWrap;
        textInput->ComponentTextData.MaxLine = MaxLine;
        textNode->TextFlags = savedTextFlags | TextFlags.WordWrap | TextFlags.MultiLine;
    }

    private unsafe void UpdateChatLog(AddonChatLog* addon) {
        var textInput = addon->TextInput;
        if (textInput is null) return;

        var textNode = ((AtkComponentInputBase*)textInput)->AtkTextNode;
        if (textNode is null) return;

        // The game re-wraps the log text when the window is resized but not the input text, so the
        // line count would be measured from a wrap that belongs to the old width.
        var textWidth = ((AtkResNode*)textNode)->Size.X;
        if (Math.Abs(textWidth - lastTextWidth) > 0.5f) {
            lastTextWidth = textWidth;
            textNode->ApplyTextFlow();
        }

        // Never taller than the room above it, so a narrow window cannot push the box past the top
        // of the chat window.
        var stockTop = ((AtkResNode*)addon->TabBarStartImageNode)->Y - ((AtkResNode*)((AtkComponentBase*)textInput)->OwnerNode)->Size.Y;
        var delta = Math.Min(MeasureExtraHeight(textNode), (int)Math.Max(0.0f, stockTop));

        // Written every frame rather than on change: the game rewrites parts of this layout on its
        // own, and only the nodes it touches would be corrected otherwise.
        currentDelta = delta;

        ApplyInputDelta(addon, delta);
        ApplyPanelDelta(addon, delta);

        UpdateCandidateWindow(textInput);
    }

    /// <summary>
    /// The game re-runs its own layout after the chat window has been resized, which lands after
    /// the update pass and stretches the box back over the log. Writing it again before the frame
    /// is drawn is what makes the box keep the height it was given.
    /// </summary>
    private unsafe void ReapplyInputDelta(AddonChatLog* addon)
        => ApplyInputDelta(addon, currentDelta);

    private unsafe void ResetChatLog(AddonChatLog* addon) {
        ApplyInputDelta(addon, 0);
        gapsKnown = false;

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

    /// <summary>
    /// Moves the input component up by the wrapped line count and grows what is drawn in it, while
    /// leaving the component's own height alone.
    ///
    /// The window height the game works out is background + input height + tab row, and it works it
    /// out again whenever the window is resized. A taller component there is added to the window,
    /// which pushes the tab row and the dropdown down with it. Keeping the height it expects while
    /// moving the component is what makes the box grow without the rest of the window following.
    ///
    /// The component is what moves, not what is inside it: the game places the caret and works out
    /// what a click landed on from a fixed origin in the component, so what is drawn has to keep
    /// the position it has within it.
    /// </summary>
    private unsafe void ApplyInputDelta(AddonChatLog* addon, int delta) {
        var textInput = addon->TextInput;
        if (textInput is null) return;

        var component = (AtkComponentBase*)textInput;
        var componentNode = (AtkResNode*)component->OwnerNode;
        var textNode = (AtkResNode*)((AtkComponentInputBase*)textInput)->AtkTextNode;
        var background = component->GetNodeById(BackgroundNodeId);
        var tabRow = (AtkResNode*)addon->TabBarStartImageNode;
        var channelText = (AtkResNode*)addon->CurrentChannelTextNode;

        if (componentNode is null || textNode is null || background is null || tabRow is null) return;

        // The box sits on the tab row, and its height is never written here, so where it belongs
        // follows from the two of them without anything being remembered.
        var stockHeight = componentNode->Size.Y;
        var stockTop = tabRow->Y - stockHeight;

        componentNode->Position = new Vector2(componentNode->X, stockTop - delta);

        // The background covers the box exactly, in every state the game leaves it in, so it needs
        // nothing measured.
        background->Size = new Vector2(background->Size.X, stockHeight + delta);

        // How much shorter the text is than the box, read while the box is not grown. A text node
        // taller than the box it sits in is one this grew on an earlier frame and has not put back
        // yet, and measuring that would bake the growth in permanently.
        if (delta is 0 && textNode->Size.Y <= stockHeight) {
            textHeightGap = stockHeight - textNode->Size.Y;
            channelTextGap = channelText is null ? 0.0f : stockTop - channelText->Y;
            gapsKnown = true;
        }

        if (!gapsKnown) return;

        // No collision node of its own: the text node is what the game hits, so it must not be
        // grown twice.
        textNode->Size = new Vector2(textNode->Size.X, stockHeight - textHeightGap + delta);

        // The channel name sits at the old top edge and would end up inside the grown box.
        if (channelText is not null) {
            channelText->Position = new Vector2(channelText->X, stockTop - delta - channelTextGap);
        }
    }

    private unsafe void ApplyPanelDelta(AddonChatLog* addon, int delta) {
        if (panelResizers is null) return;

        var textInput = addon->TextInput;
        var tabRow = (AtkResNode*)addon->TabBarStartImageNode;
        if (textInput is null || tabRow is null) return;

        var componentNode = (AtkResNode*)((AtkComponentBase*)textInput)->OwnerNode;
        if (componentNode is null) return;

        var stockTop = tabRow->Y - componentNode->Size.Y;

        foreach (var resizer in panelResizers) {
            resizer.Apply(stockTop, delta);
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
