using System;
using System.Collections.Generic;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using KamiToolKit.Classes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.ClearTextInputs;

public unsafe class ClearTextInputs : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_ClearTextInputs,
        Description = Strings.ModificationDescription_ClearTextInputs,
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "Initial Implementation"),
        ],
    };

    private Hook<AtkComponentTextInput.Delegates.Setup>? onTextComponentSetupHook;

    private CustomEventListener? customEventListener;

    private List<Pointer<AtkCollisionNode>>? registeredEventNodes;

    public override bool IsExperimental => true;

    public override void OnEnable() {
        registeredEventNodes = [];
        
        customEventListener = new CustomEventListener(HandleEvents);

        onTextComponentSetupHook = Services.Hooker.HookFromAddress<AtkComponentTextInput.Delegates.Setup>(AtkComponentTextInput.StaticVirtualTablePointer->Setup, OnTextComponentSetup);
        onTextComponentSetupHook?.Enable();
    }

    public override void OnDisable() {
        onTextComponentSetupHook?.Dispose();
        onTextComponentSetupHook = null;

        if (customEventListener is not null) {
            foreach (var node in registeredEventNodes ?? []) {
                if (node.Value is null) continue;
                if (node.Value->VirtualTable == AtkEventTarget.StaticVirtualTablePointer) continue; // Node has been disposed already

                node.Value->AtkEventManager.UnregisterEvent(AtkEventType.MouseClick, 0x80000, customEventListener, false);
            }
        }
        
        registeredEventNodes?.Clear();
        registeredEventNodes = null;
        
        customEventListener?.Dispose();
        customEventListener = null;
    }

    private void OnTextComponentSetup(AtkComponentTextInput* textInput) {
        onTextComponentSetupHook!.Original(textInput);

        try {
            if (customEventListener is null) return;

            var collisionNode = textInput->CollisionNode;
            if (collisionNode is null) return;

            var atkEventManager = collisionNode->AtkEventManager;

            atkEventManager.RegisterEvent(
                AtkEventType.MouseClick, 
                0x80000, 
                null, 
                (AtkEventTarget*)collisionNode, 
                customEventListener, 
                false
            );
            
            registeredEventNodes?.Add(collisionNode);
        }
        catch (Exception e) {
            Services.PluginLog.Error(e, "Failed to add custom input events");
        }
    }
    
    private static void HandleEvents(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        if (eventParam is not 0x80000) return;
        if (eventType is not AtkEventType.MouseClick) return;
        if (atkEventData->MouseData.ButtonId is not 1) return;
        
        Services.PluginLog.Debug("Right clicked!");

        var collisionNode = (AtkCollisionNode*)atkEvent->Target;
        if (collisionNode is null) return;

        var parent = (AtkComponentNode*)collisionNode->ParentNode;
        if (parent is null) return;

        var component = (AtkComponentTextInput*)parent->Component;
        if (component is null) return;

        var addon = component->OwnerAddon;
        if (addon is null) return;

        // Little hacky, have to unfocus else it will remember its last input string when you press another key
        AtkStage.Instance()->AtkInputManager->SetFocus(null, addon, 0);
        component->SetText(string.Empty);
        AtkStage.Instance()->AtkInputManager->SetFocus((AtkResNode*)collisionNode, addon, 0);
    }
}
