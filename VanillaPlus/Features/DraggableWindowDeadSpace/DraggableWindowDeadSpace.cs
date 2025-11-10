using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
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
            new ChangeLogInfo(1, "Initial Implementation"),
            new ChangeLogInfo(2, "Reworked feature to only apply to ui elements with window frames, removed experimental status"),
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
        if (!Services.ClientState.IsLoggedIn) return;
        
        var addon = (AtkUnitBase*)args.Addon.Address;

        if (addon->WindowNode is not null) {
            foreach (var node in addon->WindowNode->Component->UldManager.Nodes) {
                if (node.Value is null) continue;
                if (node.Value->GetNodeType() is NodeType.NineGrid) {
                    var newInteractionNode = new ResNode {
                        Size = node.Value->Size(),
                        IsVisible = true,
                    };

                    newInteractionNode.AddEvent(AtkEventType.MouseOver, () => {
                        Services.AddonEventManager.SetCursor(AddonCursorType.Hand);
                    });

                    newInteractionNode.AddEvent(AtkEventType.MouseOut, () => {
                        if (!isDragging) {
                            Services.AddonEventManager.ResetCursor();
                        }
                    });

                    newInteractionNode.AddEvent(AtkEventType.MouseClick, () => {
                        if (!isDragging) {
                            Services.AddonEventManager.SetCursor(AddonCursorType.Hand);
                        }
                    });

                    newInteractionNode.AddEvent(AtkEventType.MouseDown, OnWindowMouseDown);

                    System.NativeController.AttachNode(newInteractionNode, node, NodePosition.BeforeTarget);
                    windowInteractionNodes?.Add(args.AddonName, newInteractionNode);
                    return;
                }
            }
        }
    }

    private void OnAddonFinalize(AddonEvent type, AddonArgs args) {
        if (windowInteractionNodes?.TryGetValue(args.AddonName, out var node) ?? false) {
            System.NativeController.DetachNode(node);
            node.Dispose();
            windowInteractionNodes?.Remove(args.AddonName);
        }
    }

    private void OnWindowMouseDown(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        var targetNode = (AtkResNode*)atkEvent->Target;
        var targetAddon = RaptureAtkUnitManager.Instance()->GetAddonByNode(targetNode);

        if (targetAddon is null) return;
        
        var addonHeaderNode = targetAddon->WindowHeaderCollisionNode;
        if (addonHeaderNode is null) return;
        
        var mousePosition = atkEventData->GetMousePosition();
        
        if (addonHeaderNode->CheckCollisionAtCoords((short)mousePosition.X, (short)mousePosition.Y, true)) {
            return;
        }

        if (!isDragging) {
            dragStart = atkEventData->GetMousePosition();
            Services.AddonEventManager.SetCursor(AddonCursorType.Grab);
            cursorEventListener?.AddEvent(AtkEventType.MouseMove, (AtkResNode*) atkEvent->Target);
            cursorEventListener?.AddEvent(AtkEventType.MouseUp, (AtkResNode*) atkEvent->Target);
            isDragging = true;
        }
    }

    private void OnViewportEvent(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        if (eventType is not (AtkEventType.MouseMove or AtkEventType.MouseUp)) return;
        
        var targetAddon = RaptureAtkUnitManager.Instance()->GetAddonByNode(atkEvent->Node);
        if (targetAddon is null) return;

        ref var mouseData = ref atkEventData->MouseData;
        var mousePosition = new Vector2(mouseData.PosX, mouseData.PosY);
        
        switch (eventType) {
            case AtkEventType.MouseMove:
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
