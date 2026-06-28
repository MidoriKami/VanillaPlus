using System.Globalization;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.InventoryCooldowns;

public unsafe class InventoryCooldownTextNode : TextNode {
    public required AtkComponentDragDrop* Slot;
    public required int SlotIndex;

    protected override void Dispose(bool disposing, bool isNativeDestructor) {
        SetImageMultiply(100);
        Slot = null;

        base.Dispose(disposing, isNativeDestructor);
    }

    public void Update(InventoryItem* item) {
        if (Slot is null) return;

        // icon won't be visible when the item is being dragged
        if (Slot->AtkComponentIcon is null) return;
        if (Slot->AtkComponentIcon->OwnerNode is null) return;
        if (!Slot->AtkComponentIcon->OwnerNode->IsVisible()) {
            Hide();
            return;
        }

        if (item is null) {
            Hide();
            return;
        }

        var itemId = item->GetItemId();

        if (itemId is 0) {
            Hide();
            return;
        }

        var actionManager = ActionManager.Instance();
        var actionType = ItemUtil.IsEventItem(itemId) ? ActionType.EventItem : ActionType.Item;

        if (!actionManager->IsRecastTimerActive(actionType, itemId)) {
            Hide();
            return;
        }

        var timeLeft = actionManager->GetRecastTimeLeft(actionType, itemId);
        if (timeLeft < 0.1f) {
            Hide();
            return;
        }

        if (timeLeft > 2.0f) {
            Node->SetText(RaptureTextModule.Instance()->FormatTimeSpan((uint)timeLeft));
        }
        else {
            String = timeLeft.ToString("F1", CultureInfo.InvariantCulture);
        }

        SetImageMultiply(50);
    }

    private void Hide() {
        if (!IsVisible) return;
        Node->SetText(""u8);
        SetImageMultiply(100);
    }

    private void SetImageMultiply(byte multiply) {
        if (Slot is null) return;

        var iconComponent = Slot->AtkComponentIcon;
        if (iconComponent is null) return;

        var imageNode = iconComponent->IconImage;
        if (imageNode is null) return;

        imageNode->MultiplyBlue = multiply;
        imageNode->MultiplyRed = multiply;
        imageNode->MultiplyGreen = multiply;
    }
}
