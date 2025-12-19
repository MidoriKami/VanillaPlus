using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace VanillaPlus.Extensions;

public static unsafe class AtkStageExtensions {
    extension(ref AtkStage stage) {
        public void ShowActionTooltip(AtkResNode* node, uint actionId, string? textLabel = null) {
            using var stringBuffer = new Utf8String();

            var tooltipType = AtkTooltipManager.AtkTooltipType.Action;
        
            var tooltipArgs = stackalloc AtkTooltipManager.AtkTooltipArgs[1];
            tooltipArgs->Ctor();
            tooltipArgs->ActionArgs.Kind = DetailKind.Action;
            tooltipArgs->ActionArgs.Id = (int)actionId;
            tooltipArgs->ActionArgs.Flags = 1;

            if (textLabel is not null) {
                tooltipType |= AtkTooltipManager.AtkTooltipType.Text;
                stringBuffer.SetString(textLabel);
                tooltipArgs->TextArgs.Text = stringBuffer.StringPtr;
            }

            var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(node);
            if (addon is null) return;
        
            stage.TooltipManager.ShowTooltip(
                tooltipType,
                addon->Id,
                node,
                tooltipArgs
            );
        }
        
        public void ShowItemTooltip(AtkResNode* node, uint itemId) {
            var tooltipArgs = stackalloc AtkTooltipManager.AtkTooltipArgs[1];
            tooltipArgs->Ctor();
            tooltipArgs->ItemArgs.Kind = DetailKind.ItemId;
            tooltipArgs->ItemArgs.ItemId = (int)itemId;

            var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(node);
            if (addon is null) return;

            stage.TooltipManager.ShowTooltip(
                AtkTooltipManager.AtkTooltipType.Item,
                addon->Id,
                node,
                tooltipArgs
            );
        }
        
        public void ShowInventoryItemTooltip(AtkResNode* node, InventoryType container, short slot) {
            var tooltipArgs = stackalloc AtkTooltipManager.AtkTooltipArgs[1];
            tooltipArgs->Ctor();
            tooltipArgs->ItemArgs.Kind = DetailKind.InventoryItem;
            tooltipArgs->ItemArgs.InventoryType = container;
            tooltipArgs->ItemArgs.Slot = slot;
            tooltipArgs->ItemArgs.BuyQuantity = -1;
            tooltipArgs->ItemArgs.Flag1 = 0;

            var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(node);
            if (addon is null) return;

            stage.TooltipManager.ShowTooltip(
                AtkTooltipManager.AtkTooltipType.Item,
                addon->Id,
                node,
                tooltipArgs
            );
        }
    }
}
