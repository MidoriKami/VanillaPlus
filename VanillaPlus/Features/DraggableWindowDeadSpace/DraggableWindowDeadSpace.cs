using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using VanillaPlus.Classes;
using VanillaPlus.Enums;

namespace VanillaPlus.Features.DraggableWindowDeadSpace;

public class DraggableWindowDeadSpace : GameModification {
    public override ModificationInfo ModificationInfo => new() {
        DisplayName = Strings.ModificationDisplay_DraggableWindowDeadSpace,
        Description = Strings.ModificationDescription_DraggableWindowDeadSpace,
        Type = ModificationType.GameBehavior,
        Authors = ["MidoriKami"],
    };

    public override bool IsExperimental => true;

    private ViewportEventListener? cursorEventListener;

    private Dictionary<string, ResNode>? windowInteractionNodes;
    private Vector2 dragStart = Vector2.Zero;
    private bool isDragging;

    public override async Task OnEnableAsync() {
        windowInteractionNodes = [];

        await Service<IFramework>.Get().RunSafely(() => {
            unsafe {
                cursorEventListener = new ViewportEventListener(OnViewportEvent);
            }
        });

        Service<IAddonLifecycle>.Get().RegisterListener(AddonEvent.PostSetup, string.Empty, OnAddonSetup);
        Service<IAddonLifecycle>.Get().RegisterListener(AddonEvent.PreFinalize, string.Empty, OnAddonFinalize);
    }

    public override async Task OnDisableAsync() {
        Service<IAddonLifecycle>.Get().UnregisterListener(OnAddonSetup, OnAddonFinalize);

        await Service<IFramework>.Get().RunSafely(() => {
            cursorEventListener?.Dispose();

            foreach (var (_, node) in windowInteractionNodes ?? []) {
                node.Dispose();
            }
        });

        cursorEventListener = null;

        windowInteractionNodes?.Clear();
        windowInteractionNodes = null;
    }

    private unsafe void OnAddonSetup(AddonEvent type, AddonArgs args) {
        if (!Service<IClientState>.Get().IsLoggedIn) return;
        if (windowInteractionNodes is null) return;

        var addon = (AtkUnitBase*)args.Addon.Address;

        if (addon->WindowNode is not null) {
            foreach (var node in addon->WindowNode->Component->UldManager.Nodes) {
                if (node.Value is null) continue;
                if (node.Value->GetNodeType() is NodeType.NineGrid) {
                    var newInteractionNode = new ResNode {
                        Size = node.Value->Size,
                    };

                    newInteractionNode.AddEvent(AtkEventType.MouseOver, () => {
                        Service<IAddonEventManager>.Get().SetCursor(AddonCursorType.Hand);
                    });

                    newInteractionNode.AddEvent(AtkEventType.MouseOut, () => {
                        if (!isDragging) {
                            Service<IAddonEventManager>.Get().ResetCursor();
                        }
                    });

                    newInteractionNode.AddEvent(AtkEventType.MouseClick, () => {
                        if (!isDragging) {
                            Service<IAddonEventManager>.Get().SetCursor(AddonCursorType.Hand);
                        }
                    });

                    newInteractionNode.AddEvent(AtkEventType.MouseDown, OnWindowMouseDown);

                    newInteractionNode.AttachNode(node, NodePosition.BeforeTarget);

                    if (!windowInteractionNodes.TryAdd(args.AddonName, newInteractionNode)) {
                        windowInteractionNodes[args.AddonName].Dispose();
                        windowInteractionNodes[args.AddonName] = newInteractionNode;
                    }

                    return;
                }
            }
        }
    }

    private void OnAddonFinalize(AddonEvent type, AddonArgs args) {
        if (windowInteractionNodes?.TryGetValue(args.AddonName, out var node) ?? false) {

            // As a safety precaution, disable any drag if any tracked window is finalizing.
            if (isDragging) {
                cursorEventListener?.RemoveEvent(AtkEventType.MouseMove);
                cursorEventListener?.RemoveEvent(AtkEventType.MouseUp);
                Service<IAddonEventManager>.Get().ResetCursor();
                isDragging = false;
            }

            node.Dispose();
            windowInteractionNodes?.Remove(args.AddonName);
        }
    }

    private unsafe void OnWindowMouseDown(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        var targetNode = (AtkResNode*)atkEvent->Target;
        var targetAddon = RaptureAtkUnitManager.Instance()->GetAddonByNode(targetNode);

        if (targetAddon is null) return;

        var addonHeaderNode = targetAddon->WindowHeaderCollisionNode;
        if (addonHeaderNode is null) return;

        var mousePosition = atkEventData->MousePosition;

        if (addonHeaderNode->CheckCollisionAtCoords((short)mousePosition.X, (short)mousePosition.Y, true)) {
            return;
        }

        if (!isDragging) {
            dragStart = atkEventData->MousePosition;
            Service<IAddonEventManager>.Get().SetCursor(AddonCursorType.Grab);
            cursorEventListener?.AddEvent(AtkEventType.MouseMove, (AtkResNode*)atkEvent->Target);
            cursorEventListener?.AddEvent(AtkEventType.MouseUp, (AtkResNode*)atkEvent->Target);
            isDragging = true;
        }
    }

    private unsafe void OnViewportEvent(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
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
                Service<IAddonEventManager>.Get().ResetCursor();
                isDragging = false;
                break;
        }
    }
}
