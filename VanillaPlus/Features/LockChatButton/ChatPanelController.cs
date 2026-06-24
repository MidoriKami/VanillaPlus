using System;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;

namespace VanillaPlus.Features.LockChatButton;

public class ChatPanelController : IDisposable {
    private readonly LockChatButtonData data;
    private readonly AddonController<AddonChatLogPanel> addonController;

    private PadlockButtonNode? buttonNode;

    public unsafe ChatPanelController(LockChatButtonData data, string addonName) {
        this.data = data;

        addonController = new AddonController<AddonChatLogPanel> {
            AddonName = addonName,
            OnSetup = OnChatLogPanelSetup,
            OnFinalize = OnChatLobPanelFinalize,
            OnPreUpdate = OnChatLogPanelUpdate,
        };
    }

    public void Enable() {
        addonController.Enable();
    }

    public void Dispose() {
        addonController.Dispose();
    }

    private unsafe void OnChatLogPanelSetup(AddonChatLogPanel* addon) {
        var positioningNode = addon->GetNodeById(6);
        if (positioningNode is null) return;

        var containerNode = addon->ContainerNode;
        if (containerNode is null) return;

        var addonGlobalScale = AtkUnitBase.GetGlobalUIScale();
        var positionX = positioningNode->Position.X + 32.0f * addonGlobalScale;

        buttonNode = new PadlockButtonNode {
            Size = new Vector2(20.0f, 24.0f) * AtkUnitBase.GetGlobalUIScale(),
            IsLocked = data.IsLocked,
            Position = new Vector2(positionX, positioningNode->Y + 2.0f * addonGlobalScale),
            TextTooltip = data.IsLocked ? Strings.LockChatButton_TooltipUnlock : Strings.LockChatButton_TooltipLock,
        };

        buttonNode.OnClick = () => OnLockButtonClicked(buttonNode);
        buttonNode.AttachNode(containerNode);
    }

    private unsafe void OnChatLogPanelUpdate(AddonChatLogPanel* addon) {
        buttonNode?.IsLocked = data.IsLocked;
    }

    private unsafe void OnChatLobPanelFinalize(AddonChatLogPanel* addon) {
        buttonNode?.Dispose();
        buttonNode = null;
    }

    private void OnLockButtonClicked(PadlockButtonNode thisButton) {
        data.IsLocked = !data.IsLocked;
        Task.Run(data.Save);

        thisButton.TextTooltip = data.IsLocked ? Strings.LockChatButton_TooltipUnlock : Strings.LockChatButton_TooltipLock;
        thisButton.ShowTooltip();
    }
}
