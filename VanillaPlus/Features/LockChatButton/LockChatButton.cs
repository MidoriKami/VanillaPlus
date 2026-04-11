using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.LockChatButton;

public unsafe class LockChatButton : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Lock Chat Button",
        Description = "Adds a button to chatlogs to lock them from moving.",
        Type = ModificationType.UserInterface,
        Authors = ["MidoriKami"],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    public override string ImageName => "LockChatButton.png";

    private AddonController<AddonChatLog>? chatLogController;
    private MultiAddonController<AddonChatLogPanel>? panelController;

    private Hook<AtkUnitBase.Delegates.MoveDelta>? moveDeltaHook;
    private Hook<AtkEventListener.Delegates.ReceiveEvent>? addonControlHook;

    private Dictionary<string, PadlockButtonNode>? panelButtons;
    
    private LockChatButtonData? data;

    public override void OnEnable() {
        data = LockChatButtonData.Load();
        data.IsLocked = false;

        panelButtons = [];

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
            AddonNames = [ "ChatLogPanel_1", "ChatLogPanel_2", "ChatLogPanel_3" ],
            OnSetup = SetupChatLogPanel,
            OnFinalize = FinalizeChatLogPanel,
        };
        panelController.Enable();
    }

    private void SetupChatLog(AddonChatLog* addon) {
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
            TextTooltip = data.IsLocked ? "[VanillaPlus] Unlock Chat Movement" : "[VanillaPlus] Lock Chat Movement",
        };

        newButton.OnClick = () => OnLockButtonClicked(newButton);
        newButton.AttachNode(&addon->AtkUnitBase);

        panelButtons.Add(addon->NameString, newButton);
    }

    private void UpdateChatLog(AddonChatLog* addon) {
        if (panelButtons is null) return;
        if (!panelButtons.TryGetValue(addon->NameString, out var button)) return;

        var containerNode = addon->GetNodeById(11);
        if (containerNode is null) return;

        button.Position = new Vector2(containerNode->Position.X + 24.0f * 2.0f + 6.0f, containerNode->Position.Y + 2.0f);
    }

    private void FinalizeChatLog(AddonChatLog* addon) {
        addonControlHook?.Dispose();
        addonControlHook = null;

        if (panelButtons is null) return;
        if (!panelButtons.TryGetValue(addon->NameString, out var button)) return;

        button.Dispose();
        panelButtons.Remove(addon->NameString);
    }

    private void SetupChatLogPanel(AddonChatLogPanel* addon) {
        if (panelButtons is null) return;
        if (panelButtons?.ContainsKey(addon->NameString) ?? true) return;
        if (data is null) return;

        var positioningNode = addon->GetNodeById(6);
        if (positioningNode is null) return;

        var containerNode = addon->ContainerNode;
        if (containerNode is null) return;

        var newButton = new PadlockButtonNode {
            Size = new Vector2(20.0f, 24.0f), 
            IsLocked = data.IsLocked, 
            Position = positioningNode->Position + new Vector2(32.0f, 2.0f), 
            TextTooltip = data.IsLocked ? "[VanillaPlus] Unlock Chat Movement" : "[VanillaPlus] Lock Chat Movement",
        };

        newButton.OnClick = () => OnLockButtonClicked(newButton);
        newButton.AttachNode(containerNode);

        panelButtons.Add(addon->NameString, newButton);
    }

    private void FinalizeChatLogPanel(AddonChatLogPanel* addon) {
        if (panelButtons is null) return;
        if (!panelButtons.TryGetValue(addon->NameString, out var button)) return;

        button.Dispose();
        panelButtons.Remove(addon->NameString);
    }

    private void OnLockButtonClicked(PadlockButtonNode thisButton) {
        if (data is null) return;

        data.IsLocked = !data.IsLocked;
        data.Save();

        foreach (var buttonNode in panelButtons ?? []) {
            buttonNode.Value.IsLocked = data.IsLocked;
        }

        thisButton.TextTooltip = data.IsLocked ? "[VanillaPlus] Unlock Chat Movement" : "[VanillaPlus] Lock Chat Movement";
        thisButton.ShowTooltip();
    }

    private void OnAddonControl(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
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

    private bool OnMoveDelta(AtkUnitBase* thisPtr, short* xDelta, short* yDelta) {
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

    public override void OnDisable() {
        chatLogController?.Dispose();
        chatLogController = null;

        panelController?.Dispose();
        panelController = null;

        moveDeltaHook?.Dispose();
        moveDeltaHook = null;

        foreach (var (_, button) in panelButtons ?? []) {
            button.Dispose();
        }
        panelButtons?.Clear();
        panelButtons = null;

        data = null;
    }
}
