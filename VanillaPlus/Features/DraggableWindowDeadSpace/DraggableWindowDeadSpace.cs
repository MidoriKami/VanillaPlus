using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Events.EventDataTypes;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Extensions;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;

namespace VanillaPlus.Features.DraggableWindowDeadSpace;

public unsafe class DraggableWindowDeadSpace : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = "Draggable Window Dead Space",
        Description = "Allows clicking and dragging on window dead space to move the window.",
        Type = ModificationType.GameBehavior,
        Authors = [ "MidoriKami" ],
        ChangeLog = [
            new ChangeLogInfo(1, "InitialChangelog"),
        ],
    };

    private ViewportEventListener? cursorEventListener;

    private Dictionary<string, ResNode>? windowInteractionNodes;
    private Vector2 dragStart = Vector2.Zero;
    private bool isDragging;
    
    public override void OnEnable() {
        windowInteractionNodes = [];
        
        cursorEventListener = new ViewportEventListener(OnViewportEvent);
        
        Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, string.Empty, OnAddonSetup);
        Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, string.Empty, OnAddonFinalize);
    }

    public override void OnDisable() {
        cursorEventListener?.Dispose();
        cursorEventListener = null;

        foreach (var (_, node) in windowInteractionNodes ?? []) {
            System.NativeController.DetachNode(node);
            node.Dispose();
        }
        
        windowInteractionNodes?.Clear();
        windowInteractionNodes = null;
    }
    
    private void OnAddonSetup(AddonEvent type, AddonArgs args) {
        var addon = (AtkUnitBase*)args.Addon.Address;
        
        if (addon->WindowNode is not null) {
            var newInteractionNode = new ResNode {
                Size = new Vector2(addon->WindowNode->Width, addon->WindowNode->Height),
                Position = new Vector2(addon->WindowNode->X, addon->WindowNode->Y),
                IsVisible = true,
                EventFlagsSet = true,
            };
            
            newInteractionNode.AddEvent(AddonEventType.MouseOver, OnWindowMouseOver);
            newInteractionNode.AddEvent(AddonEventType.MouseClick, OnWindowMouseClick, addClickHelpers: false);
            newInteractionNode.AddEvent(AddonEventType.MouseOut, OnWindowMouseOut);
            newInteractionNode.AddEvent(AddonEventType.MouseDown, OnWindowMouseDown);
            
            System.NativeController.AttachNode(newInteractionNode, addon->RootNode, NodePosition.AsFirstChild);
            windowInteractionNodes?.Add(args.AddonName, newInteractionNode);
        }
    }

    private void OnAddonFinalize(AddonEvent type, AddonArgs args) {
        if (windowInteractionNodes?.TryGetValue(args.AddonName, out var node) ?? false) {
            System.NativeController.DetachNode(node);
            node.Dispose();
            windowInteractionNodes?.Remove(args.AddonName);
        }
    }
    
    private void OnWindowMouseOver(AddonEventData addonEventData) {
        Services.AddonEventManager.SetCursor(AddonCursorType.Hand);
    }

    private void OnWindowMouseClick(AddonEventData obj) {
        if (!isDragging) {
            Services.AddonEventManager.SetCursor(AddonCursorType.Hand);
            obj.SetHandled();
        }
    }

    private void OnWindowMouseDown(AddonEventData obj) {
        if (!isDragging) {
            dragStart = obj.GetMousePosition();
            Services.AddonEventManager.SetCursor(AddonCursorType.Grab);
            cursorEventListener?.AddEvent(AtkEventType.MouseMove, (AtkResNode*)obj.NodeTargetPointer);
            cursorEventListener?.AddEvent(AtkEventType.MouseUp, (AtkResNode*)obj.NodeTargetPointer);
            isDragging = true;
        }
    }
        
    private void OnWindowMouseOut(AddonEventData obj) {
        if (!isDragging) {
            Services.AddonEventManager.ResetCursor();
        }
    }

    private void OnViewportEvent(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        if (eventType is not (AtkEventType.MouseMove or AtkEventType.MouseUp)) return;
        
        var targetAddon = RaptureAtkUnitManager.Instance()->GetAddonByNode(atkEvent->Node);
        if (targetAddon is null) return;

        var addonHeaderNode = targetAddon->WindowHeaderCollisionNode;
        if (addonHeaderNode is null) return;
        
        ref var mouseData = ref atkEventData->MouseData;
        var mousePosition = new Vector2(mouseData.PosX, mouseData.PosY);
        
        switch (eventType) {
            case AtkEventType.MouseMove when !addonHeaderNode->CheckCollisionAtCoords((short)mousePosition.X, (short)mousePosition.Y, true):
                var position = new Vector2(targetAddon->X, targetAddon->Y);
                var dragDelta = dragStart - mousePosition;
                dragStart = mousePosition;
                
                var newPosition = position - dragDelta;
                targetAddon->SetPosition((short)newPosition.X, (short)newPosition.Y);
                break;
            
            case AtkEventType.MouseUp:
                cursorEventListener?.RemoveEvent(AtkEventType.MouseMove);
                cursorEventListener?.RemoveEvent(AtkEventType.MouseUp);
                Services.AddonEventManager.ResetCursor();
                isDragging = false;
                break;
        }
    }
}
