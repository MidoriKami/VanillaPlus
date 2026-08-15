using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.LockChatButton;

public class LockChatButton : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_LockChatButton,
        Description = Strings.ModificationDescription_LockChatButton,
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
    };

    public override string ImageName => "LockChatButton.png";

    private AddonController<AddonChatLog>? chatLogController;
    private PadlockButtonNode? mainPanelButton;

    private ChatPanelController? firstPanelController;
    private ChatPanelController? secondPanelController;
    private ChatPanelController? thirdPanelController;

    private Hook<AtkUnitBase.Delegates.MoveDelta>? moveDeltaHook;
    private Hook<AtkEventListener.Delegates.ReceiveEvent>? addonControlHook;

    private LockChatButtonData? data;

    public override async Task OnEnableAsync() {
        data = await LockChatButtonData.Load();

        unsafe {
            moveDeltaHook = IGameInteropProvider.Get().HookFromAddress<AtkUnitBase.Delegates.MoveDelta>(AtkUnitBase.MemberFunctionPointers.MoveDelta, OnMoveDelta);
        }

        await moveDeltaHook.EnableAsync();

        unsafe {
            chatLogController = new AddonController<AddonChatLog> {
                AddonName = "ChatLog",
                OnSetup = SetupChatLog,
                OnPreUpdate = UpdateChatLog,
                OnFinalize = FinalizeChatLog,
            };
        }

        await chatLogController.EnableAsync();

        firstPanelController = new ChatPanelController(data, "ChatLogPanel_1");
        await firstPanelController.EnableAsync();

        secondPanelController = new ChatPanelController(data, "ChatLogPanel_2");
        await secondPanelController.EnableAsync();

        thirdPanelController = new ChatPanelController(data, "ChatLogPanel_3");
        await thirdPanelController.EnableAsync();
    }

    public override async Task OnDisableAsync() {
        await addonControlHook.DisposeAsync();
        addonControlHook = null;

        await chatLogController.DisposeAsyncSafe();
        chatLogController = null;

        await firstPanelController.DisposeAsyncSafe();
        firstPanelController = null;

        await secondPanelController.DisposeAsyncSafe();
        secondPanelController = null;

        await thirdPanelController.DisposeAsyncSafe();
        thirdPanelController = null;

        await moveDeltaHook.DisposeAsync();
        moveDeltaHook = null;

        data = null;
    }

    private unsafe void SetupChatLog(AddonChatLog* addon) {
        addonControlHook = IGameInteropProvider.Get().HookFromAddress<AtkEventListener.Delegates.ReceiveEvent>(
            addon->AddonControl.AtkEventListener.VirtualTable->ReceiveEvent,
            OnAddonControl
        );
        addonControlHook.Enable();

        if (data is null) return;

        mainPanelButton = new PadlockButtonNode {
            Size = new Vector2(20.0f, 24.0f),
            IsLocked = data.IsLocked,
            TextTooltip = data.IsLocked ? Strings.LockChatButton_TooltipUnlock : Strings.LockChatButton_TooltipLock,
        };

        mainPanelButton.OnClick = () => OnLockButtonClicked(mainPanelButton);
        mainPanelButton.AttachNode(&addon->AtkUnitBase);
    }

    private unsafe void UpdateChatLog(AddonChatLog* addon) {
        if (data is null) return;

        var containerNode = addon->GetNodeById(11);
        if (containerNode is null) return;

        var addonGlobalScale = AtkUnitBase.GetGlobalUIScale();
        var positionX = containerNode->Position.X + (24.0f * 2.0f + 6.0f) * addonGlobalScale;

        mainPanelButton?.IsLocked = data.IsLocked;

        mainPanelButton?.Position = new Vector2(positionX, containerNode->Position.Y + 2.0f);
        mainPanelButton?.Scale = new Vector2(addonGlobalScale, addonGlobalScale);
    }

    private unsafe void FinalizeChatLog(AddonChatLog* addon) {
        addonControlHook?.Dispose();
        addonControlHook = null;

        mainPanelButton?.Dispose();
        mainPanelButton = null;
    }

    private void OnLockButtonClicked(PadlockButtonNode thisButton) {
        if (data is null) return;

        data.IsLocked = !data.IsLocked;
        data.Save();

        thisButton.TextTooltip = data.IsLocked ? Strings.LockChatButton_TooltipUnlock : Strings.LockChatButton_TooltipLock;
        thisButton.ShowTooltip();
    }

    private unsafe void OnAddonControl(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        try {
            if (data is { IsLocked: true }) {
                return;
            }
        }
        catch (Exception e) {
            IPluginLog.Get().Exception(e);
        }

        addonControlHook!.Original(thisPtr, eventType, eventParam, atkEvent, atkEventData);
    }

    private unsafe bool OnMoveDelta(AtkUnitBase* thisPtr, short* xDelta, short* yDelta) {
        try {
            if (data is { IsLocked: true } && thisPtr->NameString.StartsWith("ChatLog")) {
                *xDelta = 0;
                *yDelta = 0;
                return false;
            }
        }
        catch (Exception e) {
            IPluginLog.Get().Exception(e);
        }

        return moveDeltaHook!.Original(thisPtr, xDelta, yDelta);
    }
}
