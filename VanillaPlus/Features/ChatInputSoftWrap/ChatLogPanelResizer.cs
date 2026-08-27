using System;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.ChatInputSoftWrap;

/// <summary>
/// Shortens one chat log panel so the grown chat input does not cover it. Docked panels all share
/// the main window, so every one of them has to be kept in step, not just the visible one.
/// </summary>
public class ChatLogPanelResizer : IAsyncDisposable {
    private readonly string addonName;
    private readonly AddonController<AddonChatLogPanel> addonController;

    private float inputTopOffset;
    private bool captured;

    public unsafe ChatLogPanelResizer(string addonName) {
        this.addonName = addonName;

        addonController = new AddonController<AddonChatLogPanel> {
            AddonName = addonName,
            OnSetup = OnPanelSetup,
            OnFinalize = OnPanelFinalize,
        };
    }

    public async Task EnableAsync()
        => await addonController.EnableAsync();

    public async ValueTask DisposeAsync()
        => await addonController.DisposeAsyncSafe();

    /// <summary>
    /// Ends the panel where the input box begins, so the window height the game works out - log
    /// plus input plus tab row - stays where it was.
    ///
    /// How far the panel reaches past the top of an untouched box is a constant of the layout: it
    /// measured the same across window sizes twice as tall and half as wide. Reading it once is
    /// enough, and the rest follows from where the game puts the box, so nothing has to be tracked
    /// from frame to frame.
    /// </summary>
    public unsafe void Apply(float stockInputTop, int delta) {
        var panel = IGameGui.Get().GetAddonByName<AddonChatLogPanel>(addonName);
        if (panel is null || panel->AtkUnitBase.RootNode is null) return;

        // A panel dragged out of the chat window shows its own container node. It is its own window
        // then, sharing no space with the chat input. A hidden panel is left alone too: the game
        // sizes it when its tab is selected, and it is picked up on the update after that.
        if (!panel->AtkUnitBase.IsVisible || (panel->ContainerNode is not null && panel->ContainerNode->IsVisible())) {
            captured = false;
            return;
        }

        var size = panel->AtkUnitBase.RootSize;

        if (!captured) {
            if (delta is not 0) return;

            inputTopOffset = size.Y - stockInputTop;
            captured = true;
        }

        // Only shortened as far as it can still show a line of log. Past that the box grows over
        // the log instead, which is the only thing left to give in a chat window this short.
        var chatText = panel->LogViewer.ChatText;
        var minHeight = inputTopOffset + (chatText is null ? 0.0f : chatText->LineSpacing);

        var height = (ushort)Math.Max(minHeight, stockInputTop + inputTopOffset - delta);

        if (panel->AtkUnitBase.RootNode->Height != height) {
            // SetSize rather than the Resize extension: the panel has no window node, and the panel
            // lays out its own text area, scroll bar and background from this. The width is passed
            // through as it is, the game owns that.
            //
            // The displayable line count it caches is deliberately left alone. The panel rebuilds its
            // scroll bar when that count stops matching what the text area gives, so writing the
            // count this is about to arrive at is what would leave the thumb behind.
            panel->AtkUnitBase.SetSize((ushort)size.X, height);
        }

        // Read again once the panel is back to full height, never before: measuring a panel this
        // has just shortened would take the shortening for the panel's own size and keep it.
        if (delta is 0) inputTopOffset = panel->AtkUnitBase.RootNode->Size.Y - stockInputTop;
    }

    // A fresh panel carries its stock size, so nothing has been taken off it yet.
    private unsafe void OnPanelSetup(AddonChatLogPanel* addon)
        => captured = false;

    private unsafe void OnPanelFinalize(AddonChatLogPanel* addon)
        => captured = false;
}
