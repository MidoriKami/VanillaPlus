using System;
using System.Numerics;
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

    private Vector2 baseSize;
    private bool captured;
    private int appliedDelta;

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

    public unsafe void Apply(int delta) {
        var panel = IGameGui.Get().GetAddonByName<AddonChatLogPanel>(addonName);
        if (panel is null || panel->AtkUnitBase.RootNode is null) return;

        // A panel dragged out of the chat window shows its own container node. It is its own window
        // then, sharing no space with the chat input. A hidden panel is left alone too: the game
        // sizes it when its tab is selected, and it is picked up on the update after that.
        if (!panel->AtkUnitBase.IsVisible || (panel->ContainerNode is not null && panel->ContainerNode->IsVisible())) {
            captured = false;
            return;
        }

        // A size that was not written here means the game resized the panel, so what it left behind
        // is the new stock size.
        var currentSize = panel->AtkUnitBase.RootSize;
        if (!captured || Math.Abs(currentSize.Y - (baseSize.Y - appliedDelta)) > float.Epsilon) {
            baseSize = currentSize;
            appliedDelta = 0;
            captured = true;
        }

        var height = (ushort)Math.Max(1.0f, baseSize.Y - delta);
        appliedDelta = delta;

        if (panel->AtkUnitBase.RootNode->Height == height) return;

        // SetSize rather than the Resize extension: the panel has no window node, and the panel
        // lays out its own text area, scroll bar and background from this.
        panel->AtkUnitBase.SetSize((ushort)baseSize.X, height);
        InvalidateDisplayableLineCount(panel);
    }

    // A fresh panel carries its stock size, so nothing has been taken off it yet.
    private unsafe void OnPanelSetup(AddonChatLogPanel* addon)
        => captured = false;

    private unsafe void OnPanelFinalize(AddonChatLogPanel* addon) {
        Apply(0);
        captured = false;
    }

    /// <summary>
    /// The panel rebuilds its scroll bar only when the displayable line count it has cached stops
    /// matching what the text node height gives. A log line is taller than an input line, so some
    /// steps land on the same count and the thumb keeps the previous travel. Offsetting the count
    /// the panel is about to arrive at makes it rebuild on its next update, which is the path that
    /// places the thumb correctly.
    /// </summary>
    private static unsafe void InvalidateDisplayableLineCount(AddonChatLogPanel* panel) {
        var chatText = panel->LogViewer.ChatText;
        if (chatText is null) return;

        var spacing = Math.Max((ushort)1, chatText->LineSpacing);
        var lines = (ushort)(((AtkResNode*)chatText)->Height / spacing);

        panel->LogViewer.DisplayableLineCount = (ushort)(lines + 1);
    }
}
