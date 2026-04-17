using System.Globalization;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Nodes;

namespace VanillaPlus.Features.InventoryCooldowns;

public unsafe class InventoryCooldownTextNode(InventoryCooldowns feature) : TextNode {
    public int Index;
    public AtkComponentDragDrop* Slot;

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

        var containerType = item->GetInventoryType();
        var containerSlot = item->GetSlot();
        var itemId = item->GetItemId();

        if (itemId is 0) {
            Hide();
            return;
        }

        var actionManager = ActionManager.Instance();
        var actionType = ItemUtil.IsEventItem(itemId) ? ActionType.EventItem : ActionType.Item;
        var isActive = actionManager->IsRecastTimerActive(actionType, itemId);
        var timeLeft = actionManager->GetRecastTimeLeft(actionType, itemId);

        if (!isActive || timeLeft < 0.1f) {
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

    public void Hide() {
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

    protected override void Dispose(bool disposing, bool isNativeDestructor) {
        SetImageMultiply(100);
        Slot = null;
        feature.RemoveNodeFromCache(this);
        base.Dispose(disposing, isNativeDestructor);
    }
}
