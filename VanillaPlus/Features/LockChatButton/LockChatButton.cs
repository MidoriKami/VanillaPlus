using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Hooking;
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
    private MultiAddonController<AddonChatLogPanel>? panelController;

    private Hook<AtkUnitBase.Delegates.MoveDelta>? moveDeltaHook;
    private Hook<AtkEventListener.Delegates.ReceiveEvent>? addonControlHook;

    private Dictionary<string, PadlockButtonNode>? panelButtons;

    private LockChatButtonData? data;

    public override async Task OnEnableAsync() {
        data = await LockChatButtonData.Load();

        panelButtons = [];

        unsafe {
            moveDeltaHook = Services.Hooker.HookFromAddress<AtkUnitBase.Delegates.MoveDelta>(AtkUnitBase.MemberFunctionPointers.MoveDelta, OnMoveDelta);
            moveDeltaHook?.Enable();

            chatLogController = new AddonController<AddonChatLog> {
                AddonName = "ChatLog",
                OnSetup = SetupChatLog,
                OnPreUpdate = UpdateChatLog,
                OnFinalize = FinalizeChatLog,
            };
            chatLogController.Enable();

            panelController = new MultiAddonController<AddonChatLogPanel> {
                AddonNames = ["ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3"],
                OnSetup = SetupChatLogPanel,
                OnFinalize = FinalizeChatLogPanel,
            };
            panelController.Enable();
        }
    }

    public override async Task OnDisableAsync() {
        chatLogController?.Dispose();
        chatLogController = null;

        panelController?.Dispose();
        panelController = null;

        moveDeltaHook?.Dispose();
        moveDeltaHook = null;

        data = null;

        await Services.Framework.Run(() => {
            foreach (var (_, button) in panelButtons ?? []) {
                button.Dispose();
            }
            panelButtons?.Clear();
            panelButtons = null;
        });
    }

    private unsafe void SetupChatLog(AddonChatLog* addon) {
        var addonControl = (AtkAddonControl*)((byte*)addon + 0x568);

        addonControlHook = Services.Hooker.HookFromAddress<AtkEventListener.Delegates.ReceiveEvent>(
            addonControl->AtkEventListener.VirtualTable->ReceiveEvent,
            OnAddonControl
        );
        addonControlHook.Enable();

        if (data is null) return;
        if (panelButtons is null) return;
        if (panelButtons.ContainsKey(addon->NameString)) return;

        var newButton = new PadlockButtonNode {
            Size = new Vector2(20.0f, 24.0f),
            IsLocked = data.IsLocked,
            TextTooltip = data.IsLocked ? Strings.LockChatButton_TooltipUnlock : Strings.LockChatButton_TooltipLock,
        };

        newButton.OnClick = () => OnLockButtonClicked(newButton);
        newButton.AttachNode(&addon->AtkUnitBase);

        panelButtons.Add(addon->NameString, newButton);
    }

    private unsafe void UpdateChatLog(AddonChatLog* addon) {
        if (panelButtons is null) return;
        if (!panelButtons.TryGetValue(addon->NameString, out var button)) return;

        var containerNode = addon->GetNodeById(11);
        if (containerNode is null) return;

        var addonGlobalScale = AtkUnitBase.GetGlobalUIScale();
        var positionX = containerNode->Position.X + (24.0f * 2.0f + 6.0f) * addonGlobalScale;

        button.Position = new Vector2(positionX, containerNode->Position.Y + 2.0f);
        button.Scale = new Vector2(addonGlobalScale, addonGlobalScale);
    }

    private unsafe void FinalizeChatLog(AddonChatLog* addon) {
        addonControlHook?.Dispose();
        addonControlHook = null;

        if (panelButtons is null) return;
        if (!panelButtons.TryGetValue(addon->NameString, out var button)) return;

        button.Dispose();
        panelButtons.Remove(addon->NameString);
    }

    private unsafe void SetupChatLogPanel(AddonChatLogPanel* addon) {
        if (panelButtons is null) return;
        if (panelButtons?.ContainsKey(addon->NameString) ?? true) return;
        if (data is null) return;

        var positioningNode = addon->GetNodeById(6);
        if (positioningNode is null) return;

        var containerNode = addon->ContainerNode;
        if (containerNode is null) return;

        var addonGlobalScale = AtkUnitBase.GetGlobalUIScale();
        var positionX = positioningNode->Position.X + 32.0f * addonGlobalScale;

        var newButton = new PadlockButtonNode {
            Size = new Vector2(20.0f, 24.0f) * AtkUnitBase.GetGlobalUIScale(),
            IsLocked = data.IsLocked,
            Position = new Vector2(positionX, positioningNode->Y + 2.0f * addonGlobalScale),
            TextTooltip = data.IsLocked ? Strings.LockChatButton_TooltipUnlock : Strings.LockChatButton_TooltipLock,
        };

        newButton.OnClick = () => OnLockButtonClicked(newButton);
        newButton.AttachNode(containerNode);

        panelButtons.Add(addon->NameString, newButton);
    }

    private unsafe void FinalizeChatLogPanel(AddonChatLogPanel* addon) {
        if (panelButtons is null) return;
        if (!panelButtons.TryGetValue(addon->NameString, out var button)) return;

        button.Dispose();
        panelButtons.Remove(addon->NameString);
    }

    private void OnLockButtonClicked(PadlockButtonNode thisButton) {
        if (data is null) return;

        data.IsLocked = !data.IsLocked;
        Task.Run(data.Save);

        foreach (var buttonNode in panelButtons ?? []) {
            buttonNode.Value.IsLocked = data.IsLocked;
        }

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
            Services.PluginLog.Exception(e);
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
            Services.PluginLog.Exception(e);
        }

        return moveDeltaHook!.Original(thisPtr, xDelta, yDelta);
    }
}
