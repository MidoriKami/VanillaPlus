using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.System;

namespace VanillaPlus.Extensions;

public static unsafe class AtkStageExtensions {
    public static void ShowActionTooltip(this ref AtkResNode node, uint actionId, string? textLabel = null) {
        fixed (AtkResNode* nodePointer = &node) {
            AtkStage.Instance()->ShowActionTooltip(nodePointer, actionId, textLabel);
        }
    }

    private static void ShowActionTooltip(ref this AtkStage stage, AtkResNode* node, uint actionId, string? textLabel = null) {
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

    public static void ShowItemTooltip(ref this AtkResNode node, uint itemId) {
        fixed (AtkResNode* nodePointer = &node) {
            AtkStage.Instance()->ShowItemTooltip(nodePointer, itemId);
        }
    }
    
    public static void ShowItemTooltip(ref this AtkStage stage, AtkResNode* node, uint itemId) {
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

    public static void ShowInventoryItemTooltip(ref this AtkResNode node, InventoryType container, short slot) {
        fixed (AtkResNode* nodePointer = &node) {
            AtkStage.Instance()->ShowInventoryItemTooltip(nodePointer, container, slot);
        }
    }

    public static void ShowInventoryItemTooltip(this NodeBase node, InventoryType container, short slot)
        => AtkStage.Instance()->ShowInventoryItemTooltip(node, container, slot);

    private static void ShowInventoryItemTooltip(ref this AtkStage stage, AtkResNode* node, InventoryType container, short slot) {
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
